﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
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
        /// Finds member declaration node(s) containing given <paramref name="node"/>, if any.
        /// </summary>
        /// <param name="activeSpan">Span used to disambiguate member declarations if there are multiple applicable ones based on <paramref name="node"/>.</param>
        /// <remarks>
        /// The implementation has to decide what kinds of nodes in top-level match relationship represent a declaration.
        /// Every member declaration must be represented by exactly one node, but not all nodes have to represent a declaration.
        /// 
        /// May return multiple declarations if the specified <paramref name="node"/> belongs to bodies of multiple declarations,
        /// such as in VB <c>Dim a, b As New T</c> case when <paramref name="node"/> is e.g. <c>T</c>.
        /// </remarks>
        internal abstract bool TryFindMemberDeclaration(SyntaxNode? root, SyntaxNode node, TextSpan activeSpan, out OneOrMany<SyntaxNode> declarations);

        /// <summary>
        /// If the specified <paramref name="node"/> represents a member declaration returns an object that represents its body.
        /// </summary>
        /// <param name="symbol">
        ///   If specified then the returned body must belong to this symbol.
        ///
        ///   <paramref name="node"/> node itself may represent a <see cref="MemberBody"/> that doesn't belong to the <paramref name="symbol"/>.
        ///   E.g. a record copy-constructor declaration is represented by the record type declaration node,
        ///   but this node also represents the record symbol itself.
        /// </param>
        /// <returns>
        /// Null for nodes that don't represent declarations.
        /// </returns>
        internal abstract MemberBody? TryGetDeclarationBody(SyntaxNode node, ISymbol? symbol);

        /// <summary>
        /// True if the specified <paramref name="declaration"/> node shares body with another declaration.
        /// </summary>
        internal abstract bool IsDeclarationWithSharedBody(SyntaxNode declaration, ISymbol member);

        /// <summary>
        /// Returns a node that represents a body of a lambda containing specified <paramref name="node"/>,
        /// or null if the node isn't contained in a lambda. If a node is returned it must uniquely represent the lambda,
        /// i.e. be no two distinct nodes may represent the same lambda.
        /// </summary>
        protected abstract LambdaBody? FindEnclosingLambdaBody(SyntaxNode encompassingAncestor, SyntaxNode node);

        protected abstract Match<SyntaxNode> ComputeTopLevelMatch(SyntaxNode oldCompilationUnit, SyntaxNode newCompilationUnit);
        protected abstract BidirectionalMap<SyntaxNode>? ComputeParameterMap(SyntaxNode oldDeclaration, SyntaxNode newDeclaration);
        protected abstract IEnumerable<SequenceEdit> GetSyntaxSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes);

        protected abstract bool TryGetEnclosingBreakpointSpan(SyntaxToken token, out TextSpan span);

        /// <summary>
        /// Get the active span that corresponds to specified node (or its part).
        /// </summary>
        /// <param name="minLength">
        /// In case there are multiple breakpoint spans starting at the <see cref="SyntaxNode.SpanStart"/> of the <paramref name="node"/>,
        /// <paramref name="minLength"/> can be used to disambiguate between them. 
        /// The inner-most available span whose length is at least <paramref name="minLength"/> is returned.
        /// </param>
        /// <param name="statementPart">
        /// <paramref name="node"/> might have multiple active statement span. <paramref name="statementPart"/> is used to identify the 
        /// specific part.
        /// </param>
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

        protected abstract bool AreEquivalentImpl(SyntaxToken oldToken, SyntaxToken newToken);

        /// <summary>
        /// Determines if two syntax tokens are the same, disregarding trivia differences.
        /// </summary>
        private bool AreEquivalent(SyntaxToken oldToken, SyntaxToken newToken)
            => oldToken.RawKind == newToken.RawKind && oldToken.Span.Length == newToken.Span.Length && AreEquivalentImpl(oldToken, newToken);

        protected abstract bool IsNamespaceDeclaration(SyntaxNode node);
        protected abstract bool IsCompilationUnitWithGlobalStatements(SyntaxNode node);
        protected abstract bool IsGlobalStatement(SyntaxNode node);

        /// <summary>
        /// Returns all top-level type declarations (non-nested) for a given compilation unit node.
        /// </summary>
        protected abstract IEnumerable<SyntaxNode> GetTopLevelTypeDeclarations(SyntaxNode compilationUnit);

        /// <summary>
        /// Returns all symbols with declaring syntax (<see cref="GetSymbolDeclarationSyntax(ISymbol, CancellationToken)"/> must return a syntax node)
        /// associated with an edit and an actual edit kind, which may be different then the specified one.
        /// Returns an empty set if the edit is not associated with any symbols.
        /// </summary>
        protected abstract void AddSymbolEdits(
            ref TemporaryArray<(ISymbol?, ISymbol?, EditKind)> result,
            EditKind editKind,
            SyntaxNode? oldNode,
            ISymbol? oldSymbol,
            SyntaxNode? newNode,
            ISymbol? newSymbol,
            SemanticModel? oldModel,
            SemanticModel newModel,
            Match<SyntaxNode> topMatch,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            SymbolInfoCache symbolCache,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns pairs of old and new symbols associated with a given syntactic edit.
        /// </summary>
        protected abstract OneOrMany<(ISymbol? oldSymbol, ISymbol? newSymbol)> GetEditedSymbols(
            EditKind editKind,
            SyntaxNode? oldNode,
            SyntaxNode? newNode,
            SemanticModel? oldModel,
            SemanticModel newModel,
            CancellationToken cancellationToken);

        private OneOrMany<(ISymbol? oldSymbol, ISymbol? newSymbol, EditKind editKind)> GetSymbolEdits(
            EditKind editKind,
            SyntaxNode? oldNode,
            SyntaxNode? newNode,
            SemanticModel? oldModel,
            SemanticModel newModel,
            Match<SyntaxNode> topMatch,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            SymbolInfoCache symbolCache,
            CancellationToken cancellationToken)
        {
            var result = new TemporaryArray<(ISymbol?, ISymbol?, EditKind)>();

            var symbols = GetEditedSymbols(editKind, oldNode, newNode, oldModel, newModel, cancellationToken);
            foreach (var (oldSymbol, newSymbol) in symbols)
            {
                Debug.Assert(oldSymbol != null || newSymbol != null);

                // Top-level members may be matched by syntax even when their name and signature are different.
                // An update edit is created for these matches that usually produces an insert + delete semantic edits.
                //
                // If however the members were just moved to another partial type declaration and now are "accidentally" syntax-matched,
                // we shouldn't treat such update as insert and delete.
                // 
                // Instead, a simple semantic update (or no edit at all if the body has not changed, other then trivia that can be expressed as a line delta) should be created.
                // When we detect this case we break the original update edit into two edits: delete and insert.
                // The logic in the analyzer then consolidates these edits into updates across partial type declarations if applicable.
                if (editKind == EditKind.Update && GetSemanticallyMatchingNewSymbol(oldSymbol, newSymbol, newModel, symbolCache, cancellationToken) != null)
                {
                    AddSymbolEdits(ref result, EditKind.Delete, oldNode, oldSymbol, newNode: null, newSymbol: null, oldModel, newModel, topMatch, editMap, symbolCache, cancellationToken);
                    AddSymbolEdits(ref result, EditKind.Insert, oldNode: null, oldSymbol: null, newNode, newSymbol, oldModel, newModel, topMatch, editMap, symbolCache, cancellationToken);
                }
                else
                {
                    AddSymbolEdits(ref result, editKind, oldNode, oldSymbol, newNode, newSymbol, oldModel, newModel, topMatch, editMap, symbolCache, cancellationToken);
                }
            }

            return result.Count switch
            {
                0 => OneOrMany<(ISymbol?, ISymbol?, EditKind)>.Empty,
                1 => OneOrMany.Create(result[0]),
                _ => OneOrMany.Create(result.ToImmutableAndClear())
            };
        }

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

        internal string GetDisplayKindAndName(ISymbol symbol, string? displayKind = null, bool fullyQualify = false)
        {
            displayKind ??= GetDisplayKind(symbol);
            var format = fullyQualify ? s_fullyQualifiedMemberDisplayFormat : s_unqualifiedMemberDisplayFormat;

            return (symbol is IParameterSymbol { ContainingType: not { TypeKind: TypeKind.Delegate } })
                ? string.Format(
                    FeaturesResources.symbol_kind_and_name_of_member_kind_and_name,
                    displayKind,
                    symbol.Name,
                    GetDisplayKind(symbol.ContainingSymbol),
                    symbol.ContainingSymbol.ToDisplayString(format))
                : string.Format(
                    FeaturesResources.member_kind_and_name,
                    displayKind,
                    symbol.ToDisplayString(format));
        }

        internal string GetDisplayName(SyntaxNode node, EditKind editKind = EditKind.Update)
          => TryGetDisplayName(node, editKind) ?? throw ExceptionUtilities.UnexpectedValue(node.GetType().Name);

        internal string GetDisplayKind(ISymbol symbol)
            => symbol.Kind switch
            {
                SymbolKind.Event => GetDisplayName((IEventSymbol)symbol),
                SymbolKind.Field => GetDisplayName((IFieldSymbol)symbol),
                SymbolKind.Method => GetDisplayName((IMethodSymbol)symbol),
                SymbolKind.NamedType => GetDisplayName((INamedTypeSymbol)symbol),
                SymbolKind.Parameter => FeaturesResources.parameter,
                SymbolKind.Local => FeaturesResources.local_variable,
                SymbolKind.Property => GetDisplayName((IPropertySymbol)symbol),
                SymbolKind.TypeParameter => FeaturesResources.type_parameter,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            };

        internal virtual string GetDisplayName(IEventSymbol symbol)
            => FeaturesResources.event_;

        internal virtual string GetDisplayName(IPropertySymbol symbol)
            => symbol.IsAutoProperty() ? FeaturesResources.auto_property : FeaturesResources.property_;

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
        protected abstract List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, SyntaxNode root, bool isNonLeaf);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, TextSpan newStatementSpan);

        internal abstract bool HasUnsupportedOperation(IEnumerable<SyntaxNode> newNodes, [NotNullWhen(true)] out SyntaxNode? unsupportedNode, out RudeEditKind rudeEdit);

        private bool ReportUnsupportedOperations(in DiagnosticContext diagnosticContext, DeclarationBody body, CancellationToken cancellationToken)
        {
            if (HasUnsupportedOperation(body.GetDescendantNodes(IsNotLambda), out var unsupportedNode, out var rudeEdit))
            {
                diagnosticContext.Report(rudeEdit, unsupportedNode, cancellationToken);
                return true;
            }

            return false;
        }

        internal abstract void ReportOtherRudeEditsAroundActiveStatement(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap,
            SyntaxNode oldActiveStatement,
            DeclarationBody oldBody,
            SyntaxNode newActiveStatement,
            DeclarationBody newBody,
            bool isNonLeaf);

        internal abstract void ReportInsertedMemberSymbolRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol newSymbol, SyntaxNode newNode, bool insertingIntoExistingContainingType);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(DiagnosticContext diagnosticContext, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract Func<SyntaxNode, bool> IsLambda { get; }
        internal abstract Func<SyntaxNode, bool> IsNotLambda { get; }

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
            var analysisStopwatch = SharedStopwatch.StartNew();

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
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray<RudeEditDiagnostic>.Empty, syntaxError, analysisStopwatch.Elapsed, hasChanges);
                }

                if (!hasChanges)
                {
                    // The document might have been closed and reopened, which might have triggered analysis. 
                    // If the document is unchanged don't continue the analysis since 
                    // a) comparing texts is cheaper than diffing trees
                    // b) we need to ignore errors in unchanged documents

                    Log.Write("Document unchanged: '{0}'", filePath);
                    return DocumentAnalysisResults.Unchanged(newDocument.Id, filePath, analysisStopwatch.Elapsed);
                }

                // Disallow modification of a file with experimental features enabled.
                // These features may not be handled well by the analysis below.
                if (ExperimentalFeaturesEnabled(newTree))
                {
                    Log.Write("Experimental features enabled in '{0}'", filePath);

                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.ExperimentalFeaturesEnabled, default)), syntaxError: null, analysisStopwatch.Elapsed, hasChanges);
                }

                var capabilities = new EditAndContinueCapabilitiesGrantor(await lazyCapabilities.GetValueAsync(cancellationToken).ConfigureAwait(false));
                var oldActiveStatementMap = await lazyOldActiveStatementMap.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // If the document has changed at all, lets make sure Edit and Continue is supported
                if (!capabilities.Grant(EditAndContinueCapabilities.Baseline))
                {
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(
                       new RudeEditDiagnostic(RudeEditKind.NotSupportedByRuntime, default)), syntaxError: null, analysisStopwatch.Elapsed, hasChanges);
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
                    analysisStopwatch.Elapsed,
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
                    ? new RudeEditDiagnostic(RudeEditKind.SourceFileTooBig, span: default, arguments: [newDocument.FilePath])
                    : new RudeEditDiagnostic(RudeEditKind.InternalError, span: default, arguments: [newDocument.FilePath, e.ToString()]);

                // Report as "syntax error" - we can't analyze the document
                return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(diagnostic), syntaxError: null, analysisStopwatch.Elapsed, hasChanges);
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
                    if (node != null && TryFindMemberDeclaration(topMatch.OldRoot, node, oldStatementSpan, out var oldMemberDeclarations))
                    {
                        foreach (var oldMember in oldMemberDeclarations)
                        {
                            var hasPartner = topMatch.TryGetNewNode(oldMember, out var newMember);
                            Contract.ThrowIfFalse(hasPartner);

                            var oldBody = TryGetDeclarationBody(oldMember, symbol: null);
                            var newBody = TryGetDeclarationBody(newMember, symbol: null);

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
                            if (TryGetTrackedStatement(newActiveStatementSpans, i, newText, newBody, out var trackedStatement, out var trackedStatementPart))
                            {
                                // Adjust for active statements that cover more than the old member span.
                                // For example, C# variable declarators that represent field initializers:
                                //   [|public int <<F = Expr()>>;|]
                                var adjustedOldStatementStart = oldBody.ContainsActiveStatementSpan(oldStatementSpan) ? oldStatementSpan.Start : oldBody.Envelope.Start;

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
                                var ancestors = GetExceptionHandlingAncestors(newStatement, newBody.EncompassingAncestor, oldActiveStatements[i].Statement.IsNonLeaf);
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
            public readonly BidirectionalMap<SyntaxNode>? Match;
            public readonly LambdaBody? NewBody;

            public LambdaInfo(List<int> activeNodeIndices)
                : this(activeNodeIndices, null, null)
            {
            }

            private LambdaInfo(List<int>? activeNodeIndices, BidirectionalMap<SyntaxNode>? match, LambdaBody? newLambdaBody)
            {
                ActiveNodeIndices = activeNodeIndices;
                Match = match;
                NewBody = newLambdaBody;
            }

            public bool HasActiveStatement
                => ActiveNodeIndices != null;

            public LambdaInfo WithMatch(BidirectionalMap<SyntaxNode> match, LambdaBody newLambdaBody)
                => new(ActiveNodeIndices, match, newLambdaBody);
        }

        private void AnalyzeChangedMemberBody(
            SyntaxNode? oldDeclaration,
            SyntaxNode? newDeclaration,
            MemberBody? oldMemberBody,
            MemberBody? newMemberBody,
            SemanticModel? oldModel,
            SemanticModel newModel,
            ISymbol oldMember,
            ISymbol newMember,
            Compilation oldCompilation,
            SourceText newText,
            bool isMemberReplaced,
            Match<SyntaxNode> topMatch,
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
            Debug.Assert(oldMemberBody != null || newMemberBody != null);

            var diagnosticContext = CreateDiagnosticContext(diagnostics, oldMember, newMember, newDeclaration, newModel, topMatch);

            syntaxMap = null;
            var activeStatementIndices = oldMemberBody?.GetOverlappingActiveStatements(oldActiveStatements)?.ToArray() ?? [];

            if (isMemberReplaced && !activeStatementIndices.IsEmpty())
            {
                diagnosticContext.Report(RudeEditKind.ChangingNameOrSignatureOfActiveMember, cancellationToken);
            }

            try
            {
                if (newMemberBody != null)
                {
                    _testFaultInjector?.Invoke(newMemberBody.RootNodes.First());
                }

                // Populated with active lambdas and matched lambdas. 
                // Unmatched non-active lambdas are not included.
                // { old-lambda-body -> info }
                Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas = null;

                // finds leaf nodes that correspond to the old active statements:
                using var _ = ArrayBuilder<ActiveNode>.GetInstance(out var activeNodes);

                if (newMemberBody == null)
                {
                    // The name or signature has been changed and the member needs to be deleted and a new one emitted.
                    // The debugger does not support active statement remapping between two different methods, so report rude edits.
                    //
                    // The body has been deleted. Two cases:
                    // 1) The declaration is available
                    //    Example: Deleting field initializer.
                    //    We try to find another active statement that's in the user code (e.g. initializer that logically follows the deleted one or a constructor body statement).
                    //    If such active statement does not exist it is still possible to remap the deleted statement into the synthesized body of the method that replaces the current body.
                    //    (e.g. default constructor). The debugger supports active statement remapping to a method without sequence point. It will remap to the first instruction of the method.
                    // 
                    // 2) The declaration is also deleted, but a synthesized one is generated in its place and thus an update edit is issued.
                    //    Will remap active statements to the first instruction of the synthesized body (same as above).

                    foreach (var activeStatementIndex in activeStatementIndices)
                    {
                        // the declaration must exist, otherwise we wouldn't find any active statements overlapping the old body
                        Debug.Assert(oldDeclaration != null);

                        // We have already calculated the new location of this active statement when analyzing another member declaration.
                        // This may only happen when two or more member declarations share the same body (VB AsNew clause).
                        if (newActiveStatements[activeStatementIndex] != null)
                        {
                            Debug.Assert(IsDeclarationWithSharedBody(oldDeclaration, oldMember));
                            continue;
                        }

                        var newSpan = GetDeletedDeclarationActiveSpan(topMatch.Matches, oldDeclaration);
                        newActiveStatements[activeStatementIndex] = GetActiveStatementWithSpan(oldActiveStatements[activeStatementIndex], topMatch.NewRoot.SyntaxTree, newSpan, diagnostics, cancellationToken);
                        newExceptionRegions[activeStatementIndex] = ImmutableArray<SourceFileSpan>.Empty;
                    }
                }
                else
                {
                    foreach (var activeStatementIndex in activeStatementIndices)
                    {
                        Debug.Assert(oldMemberBody != null);

                        var oldStatementSpan = oldActiveStatements[activeStatementIndex].UnmappedSpan;

                        var oldStatementSyntax = oldMemberBody.FindStatement(oldStatementSpan, out var statementPart);
                        Contract.ThrowIfNull(oldStatementSyntax);

                        var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldMemberBody.EncompassingAncestor, oldStatementSyntax);
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

                        if (TryGetTrackedStatement(newActiveStatementSpans, activeStatementIndex, newText, newMemberBody, out var newStatementSyntax, out var _))
                        {
                            var newEnclosingLambdaBody = FindEnclosingLambdaBody(newMemberBody.EncompassingAncestor, newStatementSyntax);

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
                }

                var activeNodesInBody = activeNodes.Where(n => n.EnclosingLambdaBody == null).ToArray();

                var bodyMap = ComputeMatch(oldMemberBody, newMemberBody, activeNodesInBody);
                var map = IncludeLambdaBodyMaps(bodyMap, activeNodes, ref lazyActiveOrMatchedLambdas);

                var oldStateMachineInfo = oldMemberBody?.GetStateMachineInfo() ?? StateMachineInfo.None;
                var newStateMachineInfo = newMemberBody?.GetStateMachineInfo() ?? StateMachineInfo.None;

                ReportStateMachineBodyUpdateRudeEdits(diagnosticContext, bodyMap, oldStateMachineInfo, newStateMachineInfo, hasActiveStatement: activeNodesInBody.Length != 0, cancellationToken);

                ReportMemberOrLambdaBodyUpdateRudeEdits(
                    diagnosticContext,
                    oldCompilation,
                    oldDeclaration,
                    oldMember,
                    oldMemberBody,
                    oldMemberBody,
                    newDeclaration,
                    newMember,
                    newMemberBody,
                    newMemberBody,
                    capabilities,
                    oldStateMachineInfo,
                    newStateMachineInfo,
                    cancellationToken);

                ReportLambdaAndClosureRudeEdits(
                    oldModel,
                    oldMember,
                    oldMemberBody,
                    oldDeclaration,
                    newModel,
                    newMember,
                    newMemberBody,
                    newDeclaration,
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
                    oldDeclaration != null && IsDeclarationWithInitializer(oldDeclaration) ||
                    newDeclaration != null && IsDeclarationWithInitializer(newDeclaration))
                {
                    syntaxMap = CreateSyntaxMap(map.Reverse);
                }

                foreach (var activeNode in activeNodes)
                {
                    Debug.Assert(oldMemberBody != null);
                    Debug.Assert(oldDeclaration != null);

                    var activeStatementIndex = activeNode.ActiveStatementIndex;
                    var isNonLeaf = oldActiveStatements[activeStatementIndex].Statement.IsNonLeaf;
                    var isPartiallyExecuted = (oldActiveStatements[activeStatementIndex].Statement.Flags & ActiveStatementFlags.PartiallyExecuted) != 0;
                    var statementPart = activeNode.StatementPart;
                    var oldStatementSyntax = activeNode.OldNode;
                    var oldEnclosingLambdaBody = activeNode.EnclosingLambdaBody;

                    newExceptionRegions[activeStatementIndex] = ImmutableArray<SourceFileSpan>.Empty;

                    BidirectionalMap<SyntaxNode>? match;
                    DeclarationBody oldBody;
                    DeclarationBody? newBody;
                    if (oldEnclosingLambdaBody == null)
                    {
                        match = bodyMap;
                        oldBody = oldMemberBody;
                        newBody = newMemberBody;
                    }
                    else
                    {
                        Debug.Assert(lazyActiveOrMatchedLambdas != null);

                        var matchingLambdaInfo = lazyActiveOrMatchedLambdas[oldEnclosingLambdaBody];
                        match = matchingLambdaInfo.Match;
                        oldBody = oldEnclosingLambdaBody;
                        newBody = matchingLambdaInfo.NewBody;
                    }

                    bool hasMatching;
                    SyntaxNode? newStatementSyntax;
                    if (match != null)
                    {
                        Debug.Assert(newBody != null);

                        hasMatching = oldBody.TryMatchActiveStatement(newBody, oldStatementSyntax, ref statementPart, out newStatementSyntax);
                        if (!hasMatching)
                        {
                            // If the body has an empty mapping then all active statements in the body must be mapped by TryMatchActiveStatement.
                            Debug.Assert(!match.Value.Forward.IsEmpty());

                            hasMatching = match.Value.Forward.TryGetValue(oldStatementSyntax, out newStatementSyntax);
                        }
                    }
                    else
                    {
                        // Lambda match is null if lambdas can't be matched, 
                        // in such case we won't have active statement matched either.
                        hasMatching = false;
                        newStatementSyntax = null;
                    }

                    TextSpan newSpan;
                    if (hasMatching)
                    {
                        Debug.Assert(newStatementSyntax != null);
                        Debug.Assert(match != null);
                        Debug.Assert(newBody != null);

                        // The matching node doesn't produce sequence points.
                        // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
                        newSpan = FindClosestActiveSpan(newStatementSyntax, statementPart);

                        if ((isNonLeaf || isPartiallyExecuted) && !AreEquivalentActiveStatements(oldStatementSyntax, newStatementSyntax, statementPart))
                        {
                            // rude edit: non-leaf active statement changed
                            diagnostics.Add(new RudeEditDiagnostic(isNonLeaf ? RudeEditKind.ActiveStatementUpdate : RudeEditKind.PartiallyExecutedActiveStatementUpdate, newSpan));
                        }

                        // other statements around active statement:
                        ReportOtherRudeEditsAroundActiveStatement(
                            diagnostics,
                            match.Value.Reverse,
                            oldStatementSyntax,
                            oldBody,
                            newStatementSyntax,
                            newBody,
                            isNonLeaf);
                    }
                    else if (match == null)
                    {
                        Debug.Assert(oldEnclosingLambdaBody != null);
                        Debug.Assert(lazyActiveOrMatchedLambdas != null);

                        newSpan = GetDeletedNodeDiagnosticSpan(oldEnclosingLambdaBody, oldMemberBody.EncompassingAncestor, bodyMap.Forward, lazyActiveOrMatchedLambdas);

                        // Lambda containing the active statement can't be found in the new source.
                        var oldLambda = oldEnclosingLambdaBody.GetLambda();
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ActiveStatementLambdaRemoved, newSpan, oldLambda,
                            new[] { GetDisplayName(oldLambda) }));
                    }
                    else
                    {
                        newSpan = GetDeletedNodeActiveSpan(match.Value.Forward, oldStatementSyntax);

                        if (isNonLeaf || isPartiallyExecuted)
                        {
                            // rude edit: internal active statement deleted
                            diagnostics.Add(
                                new RudeEditDiagnostic(isNonLeaf ? RudeEditKind.DeleteActiveStatement : RudeEditKind.PartiallyExecutedActiveStatementDelete,
                                GetDeletedNodeDiagnosticSpan(match.Value.Forward, oldStatementSyntax),
                                arguments: new[] { FeaturesResources.code }));
                        }
                    }

                    // If there was a lambda, but we couldn't match its body to the new tree, then the lambda was
                    // removed, so we don't need to check it for active statements. If there wasn't a lambda then
                    // match here will be the same as bodyMatch.
                    if (match != null)
                    {
                        Debug.Assert(newBody != null);

                        // exception handling around the statement:
                        CalculateExceptionRegionsAroundActiveStatement(
                            match.Value.Forward,
                            oldStatementSyntax,
                            oldBody.EncompassingAncestor,
                            newStatementSyntax,
                            newBody.EncompassingAncestor,
                            newSpan,
                            activeStatementIndex,
                            isNonLeaf,
                            newExceptionRegions,
                            diagnostics,
                            cancellationToken);
                    }

                    // We have already calculated the new location of this active statement when analyzing another member declaration.
                    // This may only happen when two or more member declarations share the same body (VB AsNew clause).
                    Debug.Assert(newSpan != default);

                    if (newActiveStatements[activeStatementIndex] == null)
                    {
                        // Currently, the debugger does not support remapping to a different source file,
                        // so the new active statement must be in the syntax tree being analyzed.
                        newActiveStatements[activeStatementIndex] = GetActiveStatementWithSpan(oldActiveStatements[activeStatementIndex], newModel.SyntaxTree, newSpan, diagnostics, cancellationToken);
                    }
                    else
                    {
                        Debug.Assert(IsDeclarationWithSharedBody(oldDeclaration, oldMember));
                    }
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

                // We expect OOM to be thrown during the analysis if the number of statements is too large.
                // In such case we report a rude edit for the document. If the host is actually running out of memory,
                // it might throw another OOM here or later on.
                if (e is OutOfMemoryException)
                {
                    diagnosticContext.Report(RudeEditKind.MemberBodyTooBig, cancellationToken, arguments: new[] { newMember.Name });
                }
                else
                {
                    diagnosticContext.Report(RudeEditKind.MemberBodyInternalError, cancellationToken, arguments: new[] { newMember.Name, e.ToString() });
                }
            }
        }

        private static bool TryGetTrackedStatement(ImmutableArray<LinePositionSpan> activeStatementSpans, int index, SourceText text, MemberBody body, [NotNullWhen(true)] out SyntaxNode? trackedStatement, out int trackedStatementPart)
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
            if (!body.Envelope.Contains(trackedSpan))
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
                // TODO: consider supporting moving AS to a different file -- https://github.com/dotnet/roslyn/issues/51177.
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
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap,
            SyntaxNode oldStatementSyntax,
            SyntaxNode oldEncompassingAncestor,
            SyntaxNode? newStatementSyntax,
            SyntaxNode newEncompassingAncestor,
            TextSpan newStatementSyntaxSpan,
            int ordinal,
            bool isNonLeaf,
            ImmutableArray<ImmutableArray<SourceFileSpan>>.Builder newExceptionRegions,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            if (newStatementSyntax == null)
            {
                if (!newEncompassingAncestor.Span.Contains(newStatementSyntaxSpan.Start))
                {
                    return;
                }

                newStatementSyntax = newEncompassingAncestor.FindToken(newStatementSyntaxSpan.Start).Parent;

                Contract.ThrowIfNull(newStatementSyntax);
            }

            var oldAncestors = GetExceptionHandlingAncestors(oldStatementSyntax, oldEncompassingAncestor, isNonLeaf);
            var newAncestors = GetExceptionHandlingAncestors(newStatementSyntax, newEncompassingAncestor, isNonLeaf);

            if (oldAncestors.Count > 0 || newAncestors.Count > 0)
            {
                var edits = new MapBasedLongestCommonSubsequence<SyntaxNode>(forwardMap).GetEdits(oldAncestors, newAncestors);
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
        private BidirectionalMap<SyntaxNode> IncludeLambdaBodyMaps(
            BidirectionalMap<SyntaxNode> memberBodyMap,
            ArrayBuilder<ActiveNode> memberBodyActiveNodes,
            ref Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas)
        {
            ArrayBuilder<(BidirectionalMap<SyntaxNode> map, SyntaxNode? oldLambda)>? lambdaBodyMatches = null;
            SyntaxNode? currentOldLambda = null;
            var currentLambdaBodyMatch = -1;
            var currentBodyMatch = memberBodyMap;

            while (true)
            {
                foreach (var (oldNode, newNode) in currentBodyMatch.Forward)
                {
                    // the node is a declaration of the current lambda (we already processed it):
                    if (oldNode == currentOldLambda)
                    {
                        continue;
                    }

                    if (TryGetLambdaBodies(oldNode, out var oldLambdaBody1, out var oldLambdaBody2))
                    {
                        lambdaBodyMatches ??= ArrayBuilder<(BidirectionalMap<SyntaxNode>, SyntaxNode?)>.GetInstance();
                        lazyActiveOrMatchedLambdas ??= new Dictionary<LambdaBody, LambdaInfo>();

                        var newLambdaBody1 = oldLambdaBody1.TryGetPartnerLambdaBody(newNode);
                        if (newLambdaBody1 != null)
                        {
                            lambdaBodyMatches.Add((ComputeMatch(oldLambdaBody1, newLambdaBody1, memberBodyActiveNodes, lazyActiveOrMatchedLambdas), oldNode));
                        }

                        if (oldLambdaBody2 != null)
                        {
                            var newLambdaBody2 = oldLambdaBody2.TryGetPartnerLambdaBody(newNode);
                            if (newLambdaBody2 != null)
                            {
                                lambdaBodyMatches.Add((ComputeMatch(oldLambdaBody2, newLambdaBody2, memberBodyActiveNodes, lazyActiveOrMatchedLambdas), oldNode));
                            }
                        }
                    }
                }

                currentLambdaBodyMatch++;
                if (lambdaBodyMatches == null || currentLambdaBodyMatch == lambdaBodyMatches.Count)
                {
                    break;
                }

                (currentBodyMatch, currentOldLambda) = lambdaBodyMatches[currentLambdaBodyMatch];
            }

            if (lambdaBodyMatches == null)
            {
                return memberBodyMap;
            }

            var map = new Dictionary<SyntaxNode, SyntaxNode>();
            var reverseMap = new Dictionary<SyntaxNode, SyntaxNode>();

            // include all matches, including the root:
            map.AddRange(memberBodyMap.Forward);
            reverseMap.AddRange(memberBodyMap.Reverse);

            foreach (var (lambdaBodyMatch, _) in lambdaBodyMatches)
            {
                foreach (var (oldNode, newNode) in lambdaBodyMatch.Forward)
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

        private static BidirectionalMap<SyntaxNode> ComputeMatch(
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
        private static BidirectionalMap<SyntaxNode> ComputeMatch(DeclarationBody? oldBody, DeclarationBody? newBody, IEnumerable<ActiveNode> activeNodes)
            => (oldBody != null && newBody != null)
             ? oldBody.ComputeMatch(newBody, knownMatches: GetMatchingActiveNodes(activeNodes))
             : BidirectionalMap<SyntaxNode>.Empty;

        private void ReportStateMachineBodyUpdateRudeEdits(
            in DiagnosticContext diagnosticContext,
            BidirectionalMap<SyntaxNode> match,
            StateMachineInfo oldStateMachineInfo,
            StateMachineInfo newStateMachineInfo,
            bool hasActiveStatement,
            CancellationToken cancellationToken)
        {
            // Consider following cases:
            // 1) The new method contains yields/awaits but the old doesn't.
            //    If the method has active statements report rude edits for each inserted yield/await (insert "around" an active statement).
            // 2) The old method is async/iterator, the new method is not and it contains an active statement.
            //    Report rude edit since we can't remap IP from MoveNext to the kickoff method.
            //    Note that iterators in VB don't need to contain yield, so this case is not covered by change in number of yields.

            if (oldStateMachineInfo.HasSuspensionPoints)
            {
                foreach (var (oldNode, newNode) in match.Forward)
                {
                    ReportStateMachineSuspensionPointRudeEdits(diagnosticContext, oldNode, newNode);
                }
            }

            // It is allowed to update a regular method to an async method or an iterator.
            // The only restriction is a presence of an active statement in the method body
            // since the debugger does not support remapping active statements to a different method.
            if (hasActiveStatement && oldStateMachineInfo.IsStateMachine != newStateMachineInfo.IsStateMachine)
            {
                diagnosticContext.Report(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, cancellationToken, arguments: Array.Empty<string>());
            }

            // report removing async as rude:
            if (oldStateMachineInfo.IsAsync && !newStateMachineInfo.IsAsync)
            {
                diagnosticContext.Report(RudeEditKind.ChangingFromAsynchronousToSynchronous, cancellationToken);
            }

            // VB supports iterator lambdas/methods without yields
            if (oldStateMachineInfo.IsIterator && !newStateMachineInfo.IsIterator)
            {
                diagnosticContext.Report(RudeEditKind.ModifiersUpdate, cancellationToken);
            }
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

        public ActiveStatementExceptionRegions GetExceptionRegions(SyntaxNode root, TextSpan unmappedActiveStatementSpan, bool isNonLeaf, CancellationToken cancellationToken)
        {
            var node = root.FindToken(unmappedActiveStatementSpan.Start).Parent;
            Debug.Assert(node != null);

            var ancestors = GetExceptionHandlingAncestors(node, root, isNonLeaf);
            return GetExceptionRegions(ancestors, root.SyntaxTree, cancellationToken);
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

        private TextSpan GetDeletedNodeDiagnosticSpan(
            LambdaBody deletedLambdaBody,
            SyntaxNode oldEncompassingAncestor,
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap,
            Dictionary<LambdaBody, LambdaInfo> lambdaInfos)
        {
            var oldLambdaBody = deletedLambdaBody;
            while (true)
            {
                var oldLambda = oldLambdaBody.GetLambda();
                var oldParentLambdaBody = FindEnclosingLambdaBody(oldEncompassingAncestor, oldLambda);
                if (oldParentLambdaBody == null)
                {
                    return GetDeletedNodeDiagnosticSpan(forwardMap, oldLambda);
                }

                if (lambdaInfos.TryGetValue(oldParentLambdaBody, out var lambdaInfo) && lambdaInfo.Match != null)
                {
                    return GetDeletedNodeDiagnosticSpan(lambdaInfo.Match.Value.Forward, oldLambda);
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
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap,
            Func<SyntaxNode, bool> nodeSelector,
            SyntaxNode oldActiveStatement,
            SyntaxNode oldEncompassingAncestor,
            SyntaxNode newActiveStatement,
            SyntaxNode newEncompassingAncestor,
            Func<TSyntaxNode, TSyntaxNode, bool> areEquivalent,
            Func<TSyntaxNode, TSyntaxNode, bool>? areSimilar)
            where TSyntaxNode : SyntaxNode
        {
            var newNodes = GetAncestors(newEncompassingAncestor, newActiveStatement, nodeSelector);
            if (newNodes == null)
            {
                return;
            }

            var oldNodes = GetAncestors(oldEncompassingAncestor, oldActiveStatement, nodeSelector);

            int matchCount;
            if (oldNodes != null)
            {
                matchCount = MatchNodes(oldNodes, newNodes, diagnostics: null, reverseMap, comparer: areEquivalent);

                // Do another pass over the nodes to improve error messages.
                if (areSimilar != null && matchCount < Math.Min(oldNodes.Count, newNodes.Count))
                {
                    matchCount += MatchNodes(oldNodes, newNodes, diagnostics, reverseMap: null, comparer: areSimilar);
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
            IReadOnlyDictionary<SyntaxNode, SyntaxNode>? reverseMap,
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
                if (reverseMap == null)
                {
                    i = IndexOfEquivalent(newNode, oldNodes, oldIndex, comparer);
                }
                else if (reverseMap.TryGetValue(newNode, out var oldPartner) && comparer((TSyntaxNode)oldPartner, (TSyntaxNode)newNode))
                {
                    i = oldNodes.IndexOf(oldPartner, oldIndex);
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

            // We enumerate tokens of the body and split them into segments. Every matched member body will have at least one segment.
            // Each segment has sequence points mapped to the same file and also all lines the segment covers map to the same line delta.
            // The first token of a segment must be the first token that starts on the line. If the first segment token was in the middle line 
            // the previous token on the same line would have different line delta and we wouldn't be able to map both of them at the same time.
            // All segments are included in the segments list regardless of their line delta (even when it's 0 - i.e. the lines did not change).
            // This is necessary as we need to detect collisions of multiple segments with different deltas later on.
            //
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

                var newTokens = TryGetDeclarationBody(newNode, symbol: null)?.GetActiveTokens();
                if (newTokens == null)
                {
                    continue;
                }

                // A (rude) edit could have been made that changes whether the node may contain active statements,
                // so although the nodes match they might not have the same active tokens.
                // E.g. field declaration changed to const field declaration.
                var oldTokens = TryGetDeclarationBody(oldNode, symbol: null)?.GetActiveTokens();
                if (oldTokens == null)
                {
                    continue;
                }

                var newTokensEnum = newTokens.GetEnumerator();
                var oldTokensEnum = oldTokens.GetEnumerator();

                var lastNewToken = default(SyntaxToken);
                var lastOldStartLine = -1;
                var lastOldFilePath = (string?)null;
                var requiresUpdate = false;

                var firstSegmentIndex = segments.Count;
                var currentSegment = (path: (string?)null, oldStartLine: 0, delta: 0, firstOldToken: default(SyntaxToken), firstNewToken: default(SyntaxToken));
                var rudeEditSpan = default(TextSpan);

                // Check if the breakpoint span that covers the first node of the segment can be translated from the old to the new by adding a line delta.
                // If not we need to recompile the containing member since we are not able to produce line update for it.
                // The first node of the segment can be the first node on its line but the breakpoint span might start on the previous line.
                bool IsCurrentSegmentBreakpointSpanMappable()
                {
                    var oldToken = currentSegment.firstOldToken;
                    var newToken = currentSegment.firstNewToken;
                    Contract.ThrowIfNull(oldToken.Parent);
                    Contract.ThrowIfNull(newToken.Parent);

                    // Some nodes (e.g. const local declaration) may not be covered by a breakpoint span.
                    if (!TryGetEnclosingBreakpointSpan(oldToken, out var oldBreakpointSpan) ||
                        !TryGetEnclosingBreakpointSpan(newToken, out var newBreakpointSpan))
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
                    if (oldHasToken != newHasToken || oldHasToken && !AreEquivalent(oldTokensEnum.Current, newTokensEnum.Current))
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
                        currentSegment = (oldMappedSpan.Path, oldStartLine, lineDelta, oldTokensEnum.Current, newTokensEnum.Current);
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
                        if (newTokensEnum.Current.HasLeadingTrivia)
                        {
                            //   [token1](trailing-trivia1)(leading-trivia2)[token2]
                            //                             ~~~~~~~~~~~~~~~~~
                            rudeEditSpan = TextSpan.FromBounds(newTokensEnum.Current.FullSpan.Start, newTokensEnum.Current.SpanStart);
                        }
                        else if (lastNewToken.HasTrailingTrivia)
                        {
                            //   [token1](trailing-trivia1)[token2]
                            //           ~~~~~~~~~~~~~~~~~~
                            rudeEditSpan = TextSpan.FromBounds(lastNewToken.Span.End, newTokensEnum.Current.SpanStart);
                        }
                        else
                        {
                            // The current token is the first token of the body and has no leading trivia.
                            //   [token1]
                            //   ~~~~~~~~        
                            rudeEditSpan = newTokensEnum.Current.Span;
                        }
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

        // Ignore tuple element changes, nullability, dynamic and parameter refkinds. These type changes do not affect runtime type.
        // They only affect custom attributes or metadata flags emitted on the members - all runtimes are expected to accept
        // these updates in metadata deltas, even if they do not have any observable effect.
        private static readonly SymbolEquivalenceComparer s_runtimeSymbolEqualityComparer = new(
            AssemblyEqualityComparer.Instance, distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true);

        private static readonly SymbolEquivalenceComparer s_exactSymbolEqualityComparer = new(
            AssemblyEqualityComparer.Instance, distinguishRefFromOut: true, tupleNamesMustMatch: true, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: false);

        protected static bool SymbolsEquivalent(ISymbol oldSymbol, ISymbol newSymbol)
            => s_exactSymbolEqualityComparer.Equals(oldSymbol, newSymbol);

        protected static bool ParameterTypesEquivalent(ImmutableArray<IParameterSymbol> oldParameters, ImmutableArray<IParameterSymbol> newParameters, bool exact)
            => oldParameters.SequenceEqual(newParameters, exact, ParameterTypesEquivalent);

        protected static bool CustomModifiersEquivalent(CustomModifier oldModifier, CustomModifier newModifier, bool exact)
            => oldModifier.IsOptional == newModifier.IsOptional &&
               TypesEquivalent(oldModifier.Modifier, newModifier.Modifier, exact);

        protected static bool CustomModifiersEquivalent(ImmutableArray<CustomModifier> oldModifiers, ImmutableArray<CustomModifier> newModifiers, bool exact)
            => oldModifiers.SequenceEqual(newModifiers, exact, CustomModifiersEquivalent);

        protected static bool ReturnTypesEquivalent(IMethodSymbol oldMethod, IMethodSymbol newMethod, bool exact)
            => oldMethod.ReturnsByRef == newMethod.ReturnsByRef &&
               oldMethod.ReturnsByRefReadonly == newMethod.ReturnsByRefReadonly && // modreq emitted on the return type
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

        protected static bool ReturnTypesEquivalent(IFieldSymbol oldField, IFieldSymbol newField, bool exact)
            => CustomModifiersEquivalent(oldField.RefCustomModifiers, newField.RefCustomModifiers, exact) &&
               TypesEquivalent(oldField.Type, newField.Type, exact);

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
            => oldParameters.SequenceEqual(newParameters, exact, TypeParameterConstraintsEquivalent);

        protected static bool BaseTypesEquivalent(INamedTypeSymbol oldType, INamedTypeSymbol newType, bool exact)
            => TypesEquivalent(oldType.BaseType, newType.BaseType, exact) &&
               TypesEquivalent(oldType.AllInterfaces, newType.AllInterfaces, exact);

        protected static bool MemberOrDelegateSignaturesEquivalent(ISymbol? oldMember, ISymbol? newMember, bool exact = false)
        {
            if (oldMember == newMember)
            {
                return true;
            }

            if (oldMember == null || newMember == null || oldMember.Kind != newMember.Kind)
            {
                return false;
            }

            switch (oldMember.Kind)
            {
                case SymbolKind.Field:
                    return ReturnTypesEquivalent((IFieldSymbol)oldMember, (IFieldSymbol)newMember, exact);

                case SymbolKind.Event:
                    return ReturnTypesEquivalent((IEventSymbol)oldMember, (IEventSymbol)newMember, exact);

                case SymbolKind.Property:
                    var oldProperty = (IPropertySymbol)oldMember;
                    var newProperty = (IPropertySymbol)newMember;
                    return ParameterTypesEquivalent(oldProperty.Parameters, newProperty.Parameters, exact) &&
                           ReturnTypesEquivalent(oldProperty, newProperty, exact);

                case SymbolKind.Method:
                    var oldMethod = (IMethodSymbol)oldMember;
                    var newMethod = (IMethodSymbol)newMember;
                    return ParameterTypesEquivalent(oldMethod.Parameters, newMethod.Parameters, exact) &&
                           oldMethod.TypeParameters.Length == newMethod.TypeParameters.Length &&
                           ReturnTypesEquivalent(oldMethod, newMethod, exact);

                case SymbolKind.NamedType when oldMember is INamedTypeSymbol { DelegateInvokeMethod: { } oldInvokeMethod }:
                    var newInvokeMethod = ((INamedTypeSymbol)newMember).DelegateInvokeMethod;
                    return newInvokeMethod != null &&
                           ParameterTypesEquivalent(oldInvokeMethod.Parameters, newInvokeMethod.Parameters, exact) &&
                           ReturnTypesEquivalent(oldInvokeMethod, newInvokeMethod, exact);

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

        protected sealed class SymbolInfoCache(
            PooledDictionary<ISymbol, SymbolKey> symbolKeyCache)
        {
            public SymbolKey GetKey(ISymbol symbol, CancellationToken cancellationToken)
                => symbolKeyCache.GetOrAdd(symbol, static (symbol, cancellationToken) => SymbolKey.Create(symbol, cancellationToken), cancellationToken);
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
            using var _3 = PooledDictionary<ISymbol, SymbolKey>.GetInstance(out var symbolKeyCache);

            var symbolCache = new SymbolInfoCache(symbolKeyCache);

            try
            {
                var oldModel = (oldDocument != null) ? await oldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false) : null;
                var newModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var oldCompilation = oldModel?.Compilation ?? await oldProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = newModel.Compilation;
                var oldTree = editScript.Match.OldRoot.SyntaxTree;
                var newTree = editScript.Match.NewRoot.SyntaxTree;

                foreach (var edit in editScript.Edits)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Debug.Assert(edit.OldNode is null || edit.NewNode is null || IsNamespaceDeclaration(edit.OldNode) == IsNamespaceDeclaration(edit.NewNode));

                    // We can ignore namespace nodes in newly added documents (old model is not available) since 
                    // all newly added types in these namespaces will have their own syntax edit.
                    var symbolEdits = oldModel != null && IsNamespaceDeclaration(edit.OldNode ?? edit.NewNode!)
                        ? OneOrMany.Create(GetNamespaceSymbolEdits(oldModel, newModel, cancellationToken))
                        : GetSymbolEdits(edit.Kind, edit.OldNode, edit.NewNode, oldModel, newModel, editScript.Match, editMap, symbolCache, cancellationToken);

                    foreach (var symbolEdit in symbolEdits)
                    {
                        var (oldSymbol, newSymbol, syntacticEditKind) = symbolEdit;

                        if (syntacticEditKind == EditKind.Move)
                        {
                            Debug.Assert(oldSymbol is INamedTypeSymbol);
                            Debug.Assert(newSymbol is INamedTypeSymbol);

                            if (!processedSymbols.Add(newSymbol))
                            {
                                continue;
                            }

                            var oldSymbolInNewCompilation = symbolCache.GetKey(oldSymbol, cancellationToken).Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                            var newSymbolInOldCompilation = symbolCache.GetKey(newSymbol, cancellationToken).Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                            if (oldSymbolInNewCompilation == null || newSymbolInOldCompilation == null)
                            {
                                if (TypesEquivalent(oldSymbol.ContainingType, newSymbol.ContainingType, exact: false) &&
                                    !SymbolsEquivalent(oldSymbol.ContainingNamespace, newSymbol.ContainingNamespace))
                                {
                                    // pick the first declaration in the new file that contains the namespace change:
                                    var newTypeDeclaration = GetSymbolDeclarationSyntax(newSymbol, refs => refs.First(r => r.SyntaxTree == edit.NewNode!.SyntaxTree), cancellationToken);
                                    Debug.Assert(newTypeDeclaration != null);

                                    diagnostics.Add(new RudeEditDiagnostic(
                                        RudeEditKind.ChangingNamespace,
                                        GetDiagnosticSpan(newTypeDeclaration, EditKind.Update),
                                        newTypeDeclaration,
                                        new[] { GetDisplayName(newTypeDeclaration), oldSymbol.ContainingNamespace.ToDisplayString(), newSymbol.ContainingNamespace.ToDisplayString() }));
                                }
                                else
                                {
                                    CreateDiagnosticContext(diagnostics, oldSymbol, newSymbol, edit.NewNode, newModel, editScript.Match).
                                        Report(RudeEditKind.Move, cancellationToken);
                                }
                            }

                            continue;
                        }

                        if (!PreprocessSymbolEdit(ref oldSymbol, ref newSymbol))
                        {
                            continue;
                        }

                        var symbol = newSymbol ?? oldSymbol;
                        Contract.ThrowIfNull(symbol);

                        Func<SyntaxNode, SyntaxNode?>? syntaxMap;
                        SemanticEditKind editKind;

                        var (oldDeclaration, newDeclaration) = GetSymbolDeclarationNodes(oldSymbol, newSymbol, edit.OldNode, edit.NewNode);

                        var diagnosticContext = CreateDiagnosticContext(diagnostics, oldSymbol, newSymbol, edit.NewNode, newModel, editScript.Match);

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
                                var containingTypeSymbolKey = symbolCache.GetKey(containingType, cancellationToken);
                                oldContainingType ??= (INamedTypeSymbol?)containingTypeSymbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                                newContainingType ??= (INamedTypeSymbol?)containingTypeSymbolKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                                if (oldContainingType != null && newContainingType != null && IsReloadable(oldContainingType))
                                {
                                    if (processedSymbols.Add(newContainingType))
                                    {
                                        if (capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                        {
                                            semanticEdits.Add(SemanticEditInfo.CreateReplace(containingTypeSymbolKey,
                                                IsPartialTypeEdit(oldContainingType, newContainingType, oldTree, newTree) ? containingTypeSymbolKey : null));
                                        }
                                        else
                                        {
                                            CreateDiagnosticContext(diagnostics, oldContainingType, newContainingType, newDeclaration, newModel, editScript.Match).
                                                Report(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, cancellationToken);
                                        }
                                    }

                                    continue;
                                }
                            }

                            // Deleting a reloadable type is a rude edit, reported the same as for non-reloadable.
                            // Adding a reloadable type is a standard type addition (TODO: unless added to a reloadable type?).
                            // Making reloadable attribute non-reloadable results in a new version of the type that is
                            // not reloadable but does not update the old version in-place.
                            if (syntacticEditKind != EditKind.Delete && oldSymbol is INamedTypeSymbol oldType && newSymbol is INamedTypeSymbol newType && IsReloadable(oldType))
                            {
                                if (symbol == newType || processedSymbols.Add(newType))
                                {
                                    if (oldType.Name != newType.Name)
                                    {
                                        // https://github.com/dotnet/roslyn/issues/54886
                                        diagnosticContext.Report(RudeEditKind.Renamed, cancellationToken);
                                    }
                                    else if (oldType.Arity != newType.Arity)
                                    {
                                        // https://github.com/dotnet/roslyn/issues/54881
                                        diagnosticContext.Report(RudeEditKind.ChangingTypeParameters, cancellationToken);
                                    }
                                    else if (!capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                    {
                                        diagnosticContext.Report(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, cancellationToken);
                                    }
                                    else
                                    {
                                        var typeKey = symbolCache.GetKey(newType, cancellationToken);
                                        semanticEdits.Add(SemanticEditInfo.CreateReplace(typeKey,
                                            IsPartialTypeEdit(oldType, newType, oldTree, newTree) ? typeKey : null));
                                    }
                                }

                                continue;
                            }
                        }

                        var skipBodyAnalysis = false;

                        switch (syntacticEditKind)
                        {
                            case EditKind.Delete:
                                {
                                    Contract.ThrowIfNull(oldModel);
                                    Contract.ThrowIfNull(oldSymbol);
                                    Contract.ThrowIfNull(oldDeclaration);

                                    syntaxMap = null;

                                    // Check if the declaration has been moved from one document to another.
                                    if (newSymbol != null)
                                    {
                                        // Symbol has actually not been deleted but rather moved to another document, another partial type declaration
                                        // or replaced with an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)
                                        editKind = SemanticEditKind.Update;

                                        // Ignore the delete if there is going to be an insert corresponding to the new symbol that will create an update edit.
                                        if (DeleteEditImpliesInsertEdit(oldSymbol, newSymbol, oldCompilation, cancellationToken))
                                        {
                                            // We need to update any active statements that are in the deleted member body.
                                            ReportDeletedMemberActiveStatementsRudeEdits();
                                            continue;
                                        }

                                        if (oldSymbol.ContainingType.IsRecord &&
                                            newSymbol.ContainingType.IsRecord &&
                                            newSymbol is IPropertySymbol newProperty &&
                                            IsPrimaryConstructorParameterMatchingSymbol(newSymbol, cancellationToken))
                                        {
                                            AnalyzeRecordPropertyReplacement((IPropertySymbol)oldSymbol, newProperty, isDeleteEdit: true);
                                            skipBodyAnalysis = true;
                                            break;
                                        }

                                        // there is no insert edit for an implicit declaration, therefore we need to issue an update:
                                        break;
                                    }

                                    // If a partial method definition is deleted (and not moved to another partial type declaration, which is handled above)
                                    // so must be the implementation. An edit will be issued for the implementation change.
                                    if (newSymbol is IMethodSymbol { IsPartialDefinition: true })
                                    {
                                        continue;
                                    }

                                    var diagnosticSpan = GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, oldDeclaration);

                                    // If we got here for a global statement then the actual edit is a delete of the synthesized Main method
                                    if (IsGlobalMain(oldSymbol))
                                    {
                                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.Delete, diagnosticSpan, edit.OldNode, new[] { GetDisplayName(edit.OldNode!, EditKind.Delete) }));
                                        continue;
                                    }

                                    ReportDeletedMemberActiveStatementsRudeEdits();

                                    var rudeEditKind = RudeEditKind.Delete;

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
                                    var oldContainingType = oldSymbol.ContainingType;
                                    var containingTypeKey = symbolCache.GetKey(oldContainingType, cancellationToken);
                                    var newContainingType = (INamedTypeSymbol?)containingTypeKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                                    if (newContainingType == null)
                                    {
                                        // If a type parameter is deleted from the parameter list of a type declaration, the symbol key won't be resolved (because the arities do not match).
                                        if (oldSymbol is ITypeParameterSymbol)
                                        {
                                            diagnosticContext.Report(RudeEditKind.Delete, cancellationToken);
                                        }

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

                                    // Note: We do not report rude edits when deleting auto-properties/events of a type with a sequential or explicit layout.
                                    // The properties are updated to throw and the backing field remains in the type.
                                    // The deleted field will remain unused since adding the property/event back is a rude edit.

                                    if (IsDeclarationWithInitializer(oldDeclaration))
                                    {
                                        DeferConstructorEdit(oldContainingType, newContainingType, oldDeclaration, syntaxMap, oldSymbol.IsStatic, isMemberWithDeletedInitializer: true);
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

                                    // Note: Delete of a constructor does not need to be deferred since it does not affect other constructors.
                                    // We do need to handle deletion of a primary record constructor though.
                                    if (oldContainingType.IsRecord)
                                    {
                                        if (IsPrimaryConstructor(oldSymbol, cancellationToken))
                                        {
                                            var oldPrimaryConstructor = (IMethodSymbol)oldSymbol;

                                            // Deconstructor delete:
                                            AddDeconstructorEdits(semanticEdits, oldPrimaryConstructor, otherConstructor: null, containingTypeKey, oldCompilation, newCompilation, isParameterDelete: true, cancellationToken);

                                            // Synthesized method updates:
                                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newModel.Compilation, newContainingType, cancellationToken);
                                        }
                                        else if (oldSymbol is IParameterSymbol oldParameter && IsPrimaryConstructor(oldParameter.ContainingSymbol, cancellationToken))
                                        {
                                            AddSynthesizedMemberEditsForRecordParameterChange(semanticEdits, oldParameter, newContainingType, containingTypeKey, isParameterDelete: true, cancellationToken);
                                        }
                                    }

                                    // do not add delete edits for parameters:
                                    if (oldSymbol is IParameterSymbol or ITypeParameterSymbol)
                                    {
                                        continue;
                                    }

                                    AddDeleteEditsForMemberAndAccessors(semanticEdits, oldSymbol.PartialAsImplementation(), containingTypeKey, cancellationToken);
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

                                    if (oldSymbol != null)
                                    {
                                        // Symbol has actually not been inserted but rather moved between documents or partial type declarations,
                                        // or is replacing an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)
                                        editKind = SemanticEditKind.Update;

                                        oldContainingType = oldSymbol.ContainingType;

                                        if (oldSymbol is IPropertySymbol { ContainingType.IsRecord: true, GetMethod.IsImplicitlyDeclared: true, SetMethod.IsImplicitlyDeclared: true } oldRecordProperty &&
                                            IsPrimaryConstructorParameterMatchingSymbol(oldSymbol, cancellationToken))
                                        {
                                            AnalyzeRecordPropertyReplacement(oldRecordProperty, (IPropertySymbol)newSymbol, isDeleteEdit: false);
                                            skipBodyAnalysis = true;
                                            break;
                                        }

                                        // Handles cases when a data member explicit declaration is moved, which may change the type layout.
                                        // As of C# 12, replacing implicitly declared member with explicitly declared does not introduce a field,
                                        // but future language features might. This type layout update covers that case as well.
                                        ReportTypeLayoutUpdateRudeEdits(diagnosticContext, newSymbol, cancellationToken);

                                        break;
                                    }

                                    // If a partial method definition is inserted (and not moved to another partial type declaration, which is handled above)
                                    // so must be the implementation. An edit will be issued for the implementation change.
                                    if (newSymbol is IMethodSymbol { IsPartialDefinition: true })
                                    {
                                        continue;
                                    }

                                    if (newContainingType != null && !IsGlobalMain(newSymbol))
                                    {
                                        // The edit actually adds a new symbol into an existing or a new type.

                                        var hasAssociatedSymbolInsert =
                                            GetAssociatedMember(newSymbol) is { } newAssociatedMember &&
                                            HasEdit(editMap, GetSymbolDeclarationSyntax(newAssociatedMember, cancellationToken), EditKind.Insert);

                                        var containingTypeKey = symbolCache.GetKey(newContainingType, cancellationToken);
                                        oldContainingType = containingTypeKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol as INamedTypeSymbol;

                                        // Check rude edits for each member even if it is inserted into a new type.
                                        if (!hasAssociatedSymbolInsert && IsMember(newSymbol))
                                        {
                                            ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol, newDeclaration, insertingIntoExistingContainingType: oldContainingType != null);
                                        }

                                        if (oldContainingType == null)
                                        {
                                            // If a type parameter is inserted into the parameter list of a type declaration, the symbol key won't be resolved (because the arities do not match).
                                            if (!hasAssociatedSymbolInsert && newSymbol is ITypeParameterSymbol)
                                            {
                                                diagnosticContext.Report(RudeEditKind.Insert, cancellationToken);
                                            }

                                            // Insertion of a new symbol into a new type.
                                            // We'll produce a single insert edit for the entire type.
                                            continue;
                                        }

                                        if (!hasAssociatedSymbolInsert && !CanAddNewMemberToExistingType(newSymbol, capabilities))
                                        {
                                            diagnostics.Add(new RudeEditDiagnostic(
                                                RudeEditKind.InsertNotSupportedByRuntime,
                                                GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                                newDeclaration,
                                                arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));

                                            continue;
                                        }

                                        // Report rude edits for changes to data member changes of a type with an explicit layout.
                                        // We disallow moving a data member of a partial type with explicit layout even when it actually does not change the layout.
                                        // We could compare the exact order of the members but the scenario is unlikely to occur.
                                        ReportTypeLayoutUpdateRudeEdits(diagnosticContext, newSymbol, cancellationToken);

                                        // If a property or field is inserted into a record the synthesized members may change
                                        // (PrintMembers print all properties and fields, Equals and GHC compare all data members, etc.)
                                        if (SymbolPresenceAffectsSynthesizedRecordMembers(newSymbol))
                                        {
                                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newContainingType, cancellationToken);
                                        }

                                        if (newSymbol is IParameterSymbol newParameter &&
                                            newContainingType.IsRecord &&
                                            IsPrimaryConstructor(newParameter.ContainingSymbol, cancellationToken))
                                        {
                                            AddSynthesizedMemberEditsForRecordParameterChange(semanticEdits, newParameter, oldContainingType, containingTypeKey, isParameterDelete: false, cancellationToken);
                                        }

                                        // do not create semantic edit for parameter insert or symbols whose associated symbol is also being inserted:
                                        if (hasAssociatedSymbolInsert || newSymbol is IParameterSymbol or ITypeParameterSymbol)
                                        {
                                            continue;
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

                                        if (IsMember(newSymbol))
                                        {
                                            ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol, newDeclaration, insertingIntoExistingContainingType: false);
                                        }
                                    }

                                    Contract.ThrowIfFalse(editKind == SemanticEditKind.Insert);

                                    var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                                    if (isConstructorWithMemberInitializers || IsDeclarationWithInitializer(newDeclaration))
                                    {
                                        Contract.ThrowIfNull(newContainingType);
                                        Contract.ThrowIfNull(oldContainingType);

                                        DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, isMemberWithDeletedInitializer: false);

                                        if (isConstructorWithMemberInitializers)
                                        {
                                            // Don't add a separate semantic edit.
                                            // Edits of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                            continue;
                                        }
                                    }
                                }

                                break;

                            case EditKind.Update:
                                Contract.ThrowIfNull(oldModel);
                                Contract.ThrowIfNull(newModel);
                                Contract.ThrowIfNull(oldSymbol);
                                Contract.ThrowIfNull(newSymbol);

                                editKind = SemanticEditKind.Update;
                                syntaxMap = null;
                                break;

                            case EditKind.Reorder:
                                Contract.ThrowIfNull(oldSymbol);
                                Contract.ThrowIfNull(newSymbol);

                                ReportTypeLayoutUpdateRudeEdits(diagnosticContext, oldSymbol, cancellationToken);

                                if (oldSymbol is IParameterSymbol &&
                                    !IsMemberOrDelegateReplaced(oldSymbol.ContainingSymbol, newSymbol.ContainingSymbol) &&
                                    !capabilities.Grant(EditAndContinueCapabilities.UpdateParameters))
                                {
                                    diagnosticContext.Report(RudeEditKind.RenamingNotSupportedByRuntime, cancellationToken);
                                }

                                continue;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                        }

                        void AnalyzeRecordPropertyReplacement(IPropertySymbol oldProperty, IPropertySymbol newProperty, bool isDeleteEdit)
                        {
                            Debug.Assert(newDeclaration != null);
                            Debug.Assert(oldProperty.ContainingType.IsRecord);
                            Debug.Assert(newProperty.ContainingType.IsRecord);

                            var (customProperty, synthesizedProperty) = isDeleteEdit ? (oldProperty, newProperty) : (newProperty, oldProperty);

                            Debug.Assert(synthesizedProperty.IsSynthesizedAutoProperty());
                            Debug.Assert(synthesizedProperty.SetMethod != null);

                            // No update is needed if both properties are synthesized. The property has been updated indirectly,
                            // e.g. when a primary constructor parameter is deleted from one partial declaration and inserted into another one.
                            if (customProperty.IsSynthesizedAutoProperty())
                            {
                                return;
                            }

                            // The synthesized auto-property is `T P { get; init; } = P`.
                            // If the initializer is different from `P` the primary constructor needs to be updated.
                            // Note: we update the constructor regardless of the initializer exact shape, but we could check for it.
                            DeferConstructorEdit(oldProperty.ContainingType, newProperty.ContainingType, newDeclaration: null, syntaxMap, oldProperty.IsStatic, isMemberWithDeletedInitializer: true);

                            if (customProperty.SetMethod == null)
                            {
                                // Custom read-only property replaced with synthesized auto-property
                                if (isDeleteEdit)
                                {
                                    AddInsertEditsForMemberAndAccessors(semanticEdits, synthesizedProperty.SetMethod, cancellationToken);
                                }
                                else
                                {
                                    AddDeleteEditsForMemberAndAccessors(semanticEdits, synthesizedProperty.SetMethod, symbolCache.GetKey(oldProperty.ContainingType, cancellationToken), cancellationToken);
                                }
                            }

                            // The synthesized property replacing the deleted one will be an auto-property.
                            // If the accessor had body or the property changed accessibility then synthesized record members might be affected.
                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newProperty.ContainingType, cancellationToken);

                            // When a custom property w/o a backing field is replaced with synthesized in a type with explicit layout,
                            // the synthesized one adds a backing field, which changes the layout of the type.
                            // Note: we only report edits that add a field, not the one that remove one.
                            // The removed field remains in the type (so its layout is unchanged).
                            if (isDeleteEdit && !customProperty.IsAutoProperty())
                            {
                                ReportTypeLayoutUpdateRudeEdits(diagnosticContext, newProperty, cancellationToken);
                            }
                        }

                        void ReportDeletedMemberActiveStatementsRudeEdits()
                        {
                            Contract.ThrowIfNull(oldDeclaration);
                            Contract.ThrowIfNull(oldSymbol);

                            var oldBody = TryGetDeclarationBody(oldDeclaration, oldSymbol);
                            if (oldBody == null)
                            {
                                return;
                            }

                            var activeStatementIndices = oldBody.GetOverlappingActiveStatements(oldActiveStatements);
                            if (!activeStatementIndices.Any())
                            {
                                return;
                            }

                            TextSpan? newActiveStatementSpan = null;
                            foreach (var index in activeStatementIndices)
                            {
                                if (newActiveStatements[index] == null)
                                {
                                    newActiveStatementSpan ??= GetDeletedDeclarationActiveSpan(editScript.Match.Matches, oldDeclaration);
                                    newActiveStatements[index] = GetActiveStatementWithSpan(oldActiveStatements[index], newTree, newActiveStatementSpan.Value, diagnostics, cancellationToken);
                                    newExceptionRegions[index] = ImmutableArray<SourceFileSpan>.Empty;
                                }
                                else
                                {
                                    // active statements were mapped from a deleted declaration to another one:
                                    Debug.Assert(newSymbol != null);
                                }
                            }

                            if (newActiveStatementSpan.HasValue)
                            {
                                diagnosticContext.Report(RudeEditKind.DeleteActiveStatement, cancellationToken);
                            }
                        }

                        Contract.ThrowIfFalse(editKind is SemanticEditKind.Update or SemanticEditKind.Insert);

                        if (editKind == SemanticEditKind.Update)
                        {
                            Contract.ThrowIfNull(oldSymbol);

                            var replaceMember = IsMemberOrDelegate(oldSymbol) && IsMemberOrDelegateReplaced(oldSymbol, newSymbol);

                            if (replaceMember && oldSymbol.Name == newSymbol.Name)
                            {
                                var signatureRudeEdit = GetSignatureChangeRudeEdit(oldSymbol, newSymbol, capabilities);
                                if (signatureRudeEdit != RudeEditKind.None)
                                {
                                    diagnosticContext.Report(signatureRudeEdit, cancellationToken);
                                    continue;
                                }
                            }

                            var oldBody = (oldDeclaration != null) ? TryGetDeclarationBody(oldDeclaration, oldSymbol) : null;
                            if (!skipBodyAnalysis)
                            {
                                var newBody = (newDeclaration != null) ? TryGetDeclarationBody(newDeclaration, newSymbol) : null;
                                if (oldBody != null || newBody != null)
                                {
                                    // The old symbol's declaration syntax may be located in a different document than the old version of the current document.
                                    var oldSyntaxModel = (oldDeclaration != null)
                                        ? await oldProject.Solution.GetRequiredDocument(oldDeclaration.SyntaxTree).GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false)
                                        : oldModel;

                                    AnalyzeChangedMemberBody(
                                        oldDeclaration,
                                        newDeclaration,
                                        oldBody,
                                        newBody,
                                        oldSyntaxModel,
                                        newModel,
                                        oldSymbol,
                                        newSymbol,
                                        oldCompilation,
                                        newText,
                                        replaceMember,
                                        editScript.Match,
                                        oldActiveStatements,
                                        newActiveStatementSpans,
                                        capabilities,
                                        newActiveStatements,
                                        newExceptionRegions,
                                        diagnostics,
                                        out syntaxMap,
                                        cancellationToken);
                                }
                            }

                            AnalyzeSymbolUpdate(diagnosticContext, capabilities, semanticEdits, out var hasAttributeChange, cancellationToken);

                            if (newSymbol is IParameterSymbol or ITypeParameterSymbol)
                            {
                                // All (type) parameter changes are applied by an update created for the containing symbol.
                                continue;
                            }

                            // If a constructor changes from including initializers to not including initializers
                            // we don't need to aggregate syntax map from all initializers for the constructor update semantic edit.
                            var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                            var isOldDeclarationWithInitializer = oldDeclaration != null && IsDeclarationWithInitializer(oldDeclaration);
                            var isNewDeclarationWithInitializer = newDeclaration != null && IsDeclarationWithInitializer(newDeclaration);

                            if (isConstructorWithMemberInitializers || isOldDeclarationWithInitializer || isNewDeclarationWithInitializer)
                            {
                                DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic,
                                    isMemberWithDeletedInitializer: isOldDeclarationWithInitializer && !isNewDeclarationWithInitializer);

                                // Syntax map will be aggregated into one created for the constructor edit.
                                // It should not be set on the edit of the member with an initializer.
                                syntaxMap = null;
                            }

                            if (isConstructorWithMemberInitializers)
                            {
                                // all updates to constructors with initializers will be created later
                                continue;
                            }

                            if (replaceMember)
                            {
                                // skip for delegates, rude edits have already been reported
                                if (oldSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Delegate })
                                {
                                    // symbol insertion might change type layout:
                                    ReportTypeLayoutUpdateRudeEdits(diagnosticContext, newSymbol, cancellationToken);

                                    var containingSymbolKey = symbolCache.GetKey(oldSymbol.ContainingType, cancellationToken);
                                    AddMemberSignatureOrNameChangeEdits(semanticEdits, oldSymbol.PartialAsImplementation(), newSymbol.PartialAsImplementation(), containingSymbolKey, cancellationToken);
                                }

                                // do not emit update
                                continue;
                            }

                            // Avoid creating unnecessary updates that are easy to determine.
                            if (!hasAttributeChange && newSymbol is
                                INamedTypeSymbol { IsGenericType: false } or // changes in type parameter attributes and constraints need type update
                                IPropertySymbol { IsIndexer: false } or      // changes in parameter attributes need indexer update
                                IFieldSymbol or
                                IEventSymbol)
                            {
                                continue;
                            }

                            // While the above analysis operates on a partial definition or implementation,
                            // semantic edits must only be issued for the implementation.
                            symbol = symbol.PartialAsImplementation();
                        }

                        var symbolKey = symbolCache.GetKey(symbol, cancellationToken);

                        // Specify partial type so that all edits of the same symbol located in multiple documents can be merged later on.
                        // The partial type needs to be specified in the following cases:
                        // 1) partial method is updated (in case both implementation and definition are updated)
                        // 2) partial type is updated
                        var partialType = editKind == SemanticEditKind.Update && symbol is IMethodSymbol { PartialDefinitionPart: not null }
                            ? symbolCache.GetKey(symbol.ContainingType, cancellationToken)
                            : IsPartialTypeEdit(oldSymbol, newSymbol, oldTree, newTree)
                            ? symbolKey
                            : (SymbolKey?)null;

                        semanticEdits.Add(editKind switch
                        {
                            SemanticEditKind.Update => SemanticEditInfo.CreateUpdate(symbolKey, syntaxMap, syntaxMapTree: (syntaxMap != null) ? newModel.SyntaxTree : null, partialType),
                            SemanticEditKind.Insert => SemanticEditInfo.CreateInsert(symbolKey, partialType),
                            SemanticEditKind.Replace => SemanticEditInfo.CreateReplace(symbolKey, partialType),
                            _ => throw ExceptionUtilities.UnexpectedValue(editKind)
                        });
                    }
                }

                // Trivia edits are generated for trivia that affect active statement positions.
                foreach (var (oldEditNode, newEditNode, diagnosticSpan) in triviaEdits)
                {
                    Contract.ThrowIfNull(oldModel);
                    Contract.ThrowIfNull(newModel);

                    var triviaSymbolEdits = GetSymbolEdits(EditKind.Update, oldEditNode, newEditNode, oldModel, newModel, editScript.Match, editMap, symbolCache, cancellationToken);
                    foreach (var edit in triviaSymbolEdits)
                    {
                        var (oldSymbol, newSymbol, _) = edit;

                        if (!PreprocessSymbolEdit(ref oldSymbol, ref newSymbol))
                        {
                            // symbol already processed
                            continue;
                        }

                        Contract.ThrowIfNull(oldSymbol);
                        Contract.ThrowIfNull(newSymbol);

                        var (oldDeclaration, newDeclaration) = GetSymbolDeclarationNodes(oldSymbol, newSymbol, oldEditNode, newEditNode);
                        Contract.ThrowIfNull(oldDeclaration);
                        Contract.ThrowIfNull(newDeclaration);

                        var oldContainingType = oldSymbol.ContainingType;
                        var newContainingType = newSymbol.ContainingType;
                        if (oldContainingType != null && newContainingType != null && IsReloadable(oldContainingType))
                        {
                            if (processedSymbols.Add(newContainingType))
                            {
                                if (capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                {
                                    var oldContainingTypeKey = SymbolKey.Create(oldContainingType, cancellationToken);
                                    semanticEdits.Add(SemanticEditInfo.CreateReplace(oldContainingTypeKey,
                                        IsPartialTypeEdit(oldContainingType, newContainingType, oldTree, newTree) ? oldContainingTypeKey : null));
                                }
                                else
                                {
                                    CreateDiagnosticContext(diagnostics, oldContainingType, newContainingType, newDeclaration, newModel, editScript.Match)
                                        .Report(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, cancellationToken);
                                }
                            }

                            continue;
                        }

                        var diagnosticContext = CreateDiagnosticContext(diagnostics, oldSymbol, newSymbol, newDeclaration, newModel, editScript.Match, diagnosticSpan);

                        AnalyzeSymbolUpdate(diagnosticContext, capabilities, semanticEdits, out var _, cancellationToken);

                        // if the member doesn't have a body triva changes have no effect:
                        var oldBody = TryGetDeclarationBody(oldDeclaration, oldSymbol);
                        if (oldBody == null)
                        {
                            continue;
                        }

                        var newBody = TryGetDeclarationBody(newDeclaration, newSymbol);
                        Contract.ThrowIfNull(newBody);

                        if (ReportUnsupportedOperations(diagnosticContext, newBody, cancellationToken))
                        {
                            continue;
                        }

                        Func<SyntaxNode, SyntaxNode?>? syntaxMap = null;

                        // only trivia changed:
                        Contract.ThrowIfNull(newBody);
                        Debug.Assert(IsConstructorWithMemberInitializers(oldSymbol, cancellationToken) == IsConstructorWithMemberInitializers(newSymbol, cancellationToken));
                        Debug.Assert(IsDeclarationWithInitializer(oldDeclaration) == IsDeclarationWithInitializer(newDeclaration));

                        // We need to provide syntax map to the compiler if the member is active (see member update above):
                        var isActiveMember =
                            oldBody.GetOverlappingActiveStatements(oldActiveStatements).Any() ||
                            IsStateMachineMethod(oldDeclaration) ||
                            ContainsLambda(oldBody);

                        syntaxMap = isActiveMember ? CreateSyntaxMapForEquivalentNodes(oldBody, newBody) : null;

                        var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                        var isDeclarationWithInitializer = IsDeclarationWithInitializer(newDeclaration);

                        if (isConstructorWithMemberInitializers || isDeclarationWithInitializer)
                        {
                            Contract.ThrowIfNull(oldContainingType);
                            Contract.ThrowIfNull(newContainingType);

                            // TODO: only create syntax map if any field initializers are active/contain lambdas or this is a partial type
                            syntaxMap ??= CreateSyntaxMapForEquivalentNodes(oldBody, newBody);

                            DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, isMemberWithDeletedInitializer: false);

                            // Don't add a separate semantic edit.
                            // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                            continue;
                        }

                        // If the member changed signature or name no additional updates are needed.
                        // E.g. property accessor might have trivia changes while the property type/name is being changed.
                        if (IsMember(oldSymbol) && IsMemberOrDelegateReplaced(oldSymbol, newSymbol))
                        {
                            continue;
                        }

                        var symbolKey = symbolCache.GetKey(newSymbol, cancellationToken);

                        semanticEdits.Add(SemanticEditInfo.CreateUpdate(
                            symbolKey,
                            syntaxMap,
                            syntaxMapTree: (syntaxMap != null) ? newTree : null,
                            partialType: IsPartialTypeEdit(oldSymbol, newSymbol, oldTree, newTree) ? symbolKey : null));
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
                        isStatic: true,
                        semanticEdits,
                        diagnostics,
                        cancellationToken);
                }

                bool PreprocessSymbolEdit(ref ISymbol? oldSymbol, ref ISymbol? newSymbol)
                {
                    Contract.ThrowIfFalse(oldSymbol != null || newSymbol != null);

                    oldSymbol ??= Resolve(newSymbol!, symbolCache.GetKey(newSymbol!, cancellationToken), oldCompilation, cancellationToken);
                    newSymbol ??= Resolve(oldSymbol!, symbolCache.GetKey(oldSymbol!, cancellationToken), newCompilation, cancellationToken);

                    static ISymbol? Resolve(ISymbol symbol, SymbolKey symbolKey, Compilation compilation, CancellationToken cancellationToken)
                    {
                        // Ignore ambiguous resolution result - it may happen if there are semantic errors in the compilation.
                        var result = symbolKey.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                        // If we were looking for a definition and an implementation is returned the definition does not exist.
                        return symbol is IMethodSymbol { PartialDefinitionPart: not null } && result is IMethodSymbol { IsPartialDefinition: true } ? null : result;
                    }

                    var symbol = newSymbol ?? oldSymbol;
                    Contract.ThrowIfNull(symbol);

                    return processedSymbols.Add(symbol);
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
                => (oldDeclaration: (oldSymbol != null && GetSingleSymbolDeclarationSyntax(oldSymbol, cancellationToken) is { } oldDeclaration) ? oldDeclaration : oldNode,
                    newDeclaration: (newSymbol != null && GetSingleSymbolDeclarationSyntax(newSymbol, cancellationToken) is { } newDeclaration) ? newDeclaration : newNode);
        }

        protected static bool IsMemberOrDelegateReplaced(ISymbol oldMember, ISymbol newMember)
            => oldMember.Name != newMember.Name ||
               !MemberOrDelegateSignaturesEquivalent(oldMember, newMember, exact: false);

        protected static bool IsMember(ISymbol symbol)
            => symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event;

        protected static bool IsMemberOrDelegate(ISymbol symbol)
            => IsMember(symbol) || symbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate };

        protected static ISymbol? GetSemanticallyMatchingNewSymbol(ISymbol? oldSymbol, ISymbol? newSymbol, SemanticModel newModel, SymbolInfoCache symbolCache, CancellationToken cancellationToken)
            => oldSymbol != null && IsMember(oldSymbol) &&
               newSymbol != null && IsMember(newSymbol) &&
               symbolCache.GetKey(oldSymbol, cancellationToken).Resolve(newModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol is { } matchingNewSymbol &&
               !matchingNewSymbol.IsSynthesized() &&
               matchingNewSymbol != newSymbol
               ? matchingNewSymbol
               : null;

        protected static void AddMemberUpdate(ref TemporaryArray<(ISymbol?, ISymbol?, EditKind)> result, ISymbol? oldSymbol, ISymbol? newSymbol, ISymbol? newSemanticallyMatchingSymbol)
        {
            if (newSemanticallyMatchingSymbol != null)
            {
                Debug.Assert(oldSymbol != null);
                Debug.Assert(newSymbol != null);

                result.Add((oldSymbol, null, EditKind.Delete));
                result.Add((null, newSymbol, EditKind.Insert));
            }
            else if (oldSymbol != null || newSymbol != null)
            {
                result.Add((oldSymbol, newSymbol, EditKind.Update));
            }
        }

        /// <summary>
        /// Adds edits of synthesized members that may be affected by a <paramref name="parameterSymbol"/> change.
        /// </summary>
        private static void AddSynthesizedMemberEditsForRecordParameterChange(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IParameterSymbol parameterSymbol,
            INamedTypeSymbol otherContainingType,
            SymbolKey containingTypeKey,
            bool isParameterDelete,
            CancellationToken cancellationToken)
        {
            var member = parameterSymbol.ContainingSymbol;
            Debug.Assert(member is IPropertySymbol or IMethodSymbol);

            // Parameter deleted from or inserted into a primary constructor of a record type.
            //
            // Note that although the compiler emits auto-properties and deconstructor automatically
            // given just an insert or udpate edit of the primary constructor, it will not emit the necessary deletes.
            // We could only add delete edits and avoid adding updates and inserts. It would make the code somewhat simpler
            // but also asymetric (delete vs insert). Adding inserts and updates to the edits explicitly also avoids
            // dependency on the compiler implementation details.
            var primaryConstructor = (IMethodSymbol)member;

            // Delete/insert/update synthesized properties and their accessors.

            // If deleting a parameter from or inserting a parameter to primary constructor of a record
            // that does not have a corresponding synthesized property (has a custom property of field)
            // has no effect on the property.

            var synthesizedProperty = GetPropertySynthesizedForRecordPrimaryConstructorParameter(parameterSymbol);
            if (synthesizedProperty != null)
            {
                var otherMembersOfParameterName = otherContainingType.GetMembers(parameterSymbol.Name);
                if (otherMembersOfParameterName.Any(static m => m is IPropertySymbol))
                {
                    // Replace a synthesized auto-property with a custom implementation:
                    AddUpdateEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, cancellationToken);
                }
                else if (isParameterDelete)
                {
                    // Delete synthesized property:
                    AddDeleteEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, deletedSymbolContainer: containingTypeKey, cancellationToken);
                }
                else
                {
                    // Insert synthesized property:
                    AddInsertEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, cancellationToken);
                }
            }
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
                        AddUpdateEditsForMemberAndAccessors(semanticEdits, deconstructor, cancellationToken);
                    }
                    else if (isDelete)
                    {
                        // Delete synthesized deconstructor:
                        AddDeleteEditsForMemberAndAccessors(semanticEdits, deconstructor, deletedSymbolContainer: containingTypeKey, cancellationToken);
                    }
                    else
                    {
                        // Insert synthesized deconstructor:
                        AddInsertEditsForMemberAndAccessors(semanticEdits, deconstructor, cancellationToken);
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

            // We don't allow deleting members from interfaces
            if (symbol.ContainingType is { TypeKind: TypeKind.Interface })
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

            // Can only delete event with explicitly declared accessors (otherwise a private field is generated that can't be deleted)
            return symbol is
                IParameterSymbol or
                ITypeParameterSymbol or
                IPropertySymbol or
                IEventSymbol { AddMethod.IsImplicitlyDeclared: false, RemoveMethod.IsImplicitlyDeclared: false };
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Update"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddUpdateEditsForMemberAndAccessors(ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol symbol, CancellationToken cancellationToken)
        {
            switch (symbol)
            {
                case IMethodSymbol or IFieldSymbol:
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

                Debug.Assert(symbol is not IMethodSymbol { IsPartialDefinition: true });

                semanticEdits.Add(SemanticEditInfo.CreateUpdate(SymbolKey.Create(symbol, cancellationToken), syntaxMap: null, syntaxMapTree: null, partialType: null));
            }
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Delete"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddDeleteEditsForMemberAndAccessors(ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol oldSymbol, SymbolKey deletedSymbolContainer, CancellationToken cancellationToken)
        {
            switch (oldSymbol)
            {
                case IMethodSymbol or IFieldSymbol:
                    AddDelete(oldSymbol);
                    break;

                case IPropertySymbol propertySymbol:
                    // Delete accessors individually, because we actually just update them to be throwing.
                    AddDelete(propertySymbol);
                    AddDelete(propertySymbol.GetMethod);
                    AddDelete(propertySymbol.SetMethod);
                    break;

                case IEventSymbol eventSymbol:
                    // Delete accessors individually, because we actually just update them to be throwing.
                    AddDelete(eventSymbol);
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

                Debug.Assert(symbol is not IMethodSymbol { IsPartialDefinition: true });

                var partialType = symbol is IMethodSymbol { PartialDefinitionPart: not null } ? SymbolKey.Create(symbol.ContainingType, cancellationToken) : (SymbolKey?)null;
                semanticEdits.Add(SemanticEditInfo.CreateDelete(SymbolKey.Create(symbol, cancellationToken), deletedSymbolContainer, partialType));
            }
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Insert"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddInsertEditsForMemberAndAccessors(ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol newSymbol, CancellationToken cancellationToken)
        {
            // When inserting a new property, we need to insert the entire property, so
            // that the backing field (if any), property and method semantics metadata tables can all be updated if/as necessary.
            // 
            // When inserting a new event we need to insert the entire event, so
            // pevent and method semantics metadata tables can all be updated if/as necessary.

            var partialType = newSymbol is IMethodSymbol { PartialDefinitionPart: not null } ? SymbolKey.Create(newSymbol.ContainingType, cancellationToken) : (SymbolKey?)null;
            semanticEdits.Add(SemanticEditInfo.CreateInsert(SymbolKey.Create(newSymbol, cancellationToken), partialType));
        }

        private static void AddMemberSignatureOrNameChangeEdits(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SymbolKey containingSymbolKey,
            CancellationToken cancellationToken)
        {
            if (oldSymbol.Name != newSymbol.Name || oldSymbol is IMethodSymbol or IFieldSymbol)
            {
                AddDeleteEditsForMemberAndAccessors(semanticEdits, oldSymbol, containingSymbolKey, cancellationToken);
                AddInsertEditsForMemberAndAccessors(semanticEdits, newSymbol, cancellationToken);
                return;
            }

            switch (oldSymbol)
            {
                case IPropertySymbol oldPropertySymbol:
                    // Properties may be overloaded on signature.

                    // delete the property and its accessors
                    AddDelete(oldPropertySymbol);
                    AddDelete(oldPropertySymbol.GetMethod);
                    AddDelete(oldPropertySymbol.SetMethod);

                    // insert new property:
                    AddInsert(newSymbol);
                    break;

                case IEventSymbol oldEventSymbol:
                    // Events can't be overloaded on their type.

                    // Update the event to associate it with the new accessors
                    semanticEdits.Add(SemanticEditInfo.CreateUpdate(SymbolKey.Create(oldSymbol, cancellationToken), syntaxMap: null, syntaxMapTree: null, partialType: null));

                    // Do not change raise since its signature is not impacted by the event type change.

                    // Update old bodies of add and remove to throw.
                    AddDelete(oldEventSymbol.AddMethod);
                    AddDelete(oldEventSymbol.RemoveMethod);

                    // Insert new add and remove:
                    var newEventSymbol = (IEventSymbol)newSymbol;
                    AddInsert(newEventSymbol.AddMethod);
                    AddInsert(newEventSymbol.RemoveMethod);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldSymbol.Kind);
            }

            void AddInsert(ISymbol? symbol)
            {
                if (symbol is null)
                    return;

                semanticEdits.Add(SemanticEditInfo.CreateInsert(SymbolKey.Create(symbol, cancellationToken), partialType: null));
            }

            void AddDelete(ISymbol? symbol)
            {
                if (symbol is null)
                    return;

                semanticEdits.Add(SemanticEditInfo.CreateDelete(SymbolKey.Create(symbol, cancellationToken), containingSymbolKey, partialType: null));
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
            in DiagnosticContext diagnosticContext,
            Compilation oldCompilation,
            SyntaxNode? oldDeclaration,
            ISymbol oldMember,
            MemberBody? oldMemberBody,
            DeclarationBody? oldBody,
            SyntaxNode? newDeclaration,
            ISymbol newMember,
            MemberBody? newMemberBody,
            DeclarationBody? newBody,
            EditAndContinueCapabilitiesGrantor capabilities,
            StateMachineInfo oldStateMachineInfo,
            StateMachineInfo newStateMachineInfo,
            CancellationToken cancellationToken)
        {
            Debug.Assert(oldBody == null || oldDeclaration != null && oldMemberBody != null);
            Debug.Assert(newBody == null || newDeclaration != null && newMemberBody != null);

            // Report rude edit if an unsupported operations is found in the new or old body. 
            // Only report for the new body if both bodies have unsupported operations.
            _ = newBody != null && ReportUnsupportedOperations(diagnosticContext, newBody, cancellationToken) ||
                oldBody != null && ReportUnsupportedOperations(diagnosticContext, oldBody, cancellationToken);

            if (oldStateMachineInfo.IsStateMachine)
            {
                ReportMissingStateMachineAttribute(diagnosticContext, oldCompilation, oldStateMachineInfo, cancellationToken);
            }

            if (!oldStateMachineInfo.IsStateMachine &&
                newStateMachineInfo.IsStateMachine &&
                !capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
            {
                // Adding a state machine, either for async or iterator, will require creating a new helper class
                // so is a rude edit if the runtime doesn't support it
                var rudeEdit = newStateMachineInfo.IsAsync ? RudeEditKind.MakeMethodAsyncNotSupportedByRuntime : RudeEditKind.MakeMethodIteratorNotSupportedByRuntime;
                diagnosticContext.Report(rudeEdit, cancellationToken, arguments: Array.Empty<string>());
            }

            if (oldStateMachineInfo.IsStateMachine && newStateMachineInfo.IsStateMachine)
            {
                if (!capabilities.Grant(EditAndContinueCapabilities.AddInstanceFieldToExistingType))
                {
                    diagnosticContext.Report(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, cancellationToken, arguments: Array.Empty<string>());
                }

                if ((InGenericContext(oldMember) ||
                     InGenericContext(newMember) ||
                     oldBody is LambdaBody && InGenericLocalContext(oldDeclaration!, oldMemberBody!.RootNodes) ||
                     newBody is LambdaBody && InGenericLocalContext(newDeclaration!, newMemberBody!.RootNodes)) &&
                    !capabilities.Grant(EditAndContinueCapabilities.GenericAddFieldToExistingType))
                {
                    diagnosticContext.Report(RudeEditKind.UpdatingGenericNotSupportedByRuntime, cancellationToken);
                }
            }
        }

        private void ReportUpdatedSymbolDeclarationRudeEdits(
            in DiagnosticContext diagnosticContext,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasGeneratedAttributeChange,
            out bool hasGeneratedReturnTypeAttributeChange,
            CancellationToken cancellationToken)
        {
            var rudeEdit = RudeEditKind.None;
            var oldSymbol = diagnosticContext.RequiredOldSymbol;
            var newSymbol = diagnosticContext.RequiredNewSymbol;

            hasGeneratedAttributeChange = false;
            hasGeneratedReturnTypeAttributeChange = false;

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
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities))
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
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities))
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
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities))
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

                if (!IsMemberOrDelegateReplaced(oldField, newField))
                {
                    Debug.Assert(ReturnTypesEquivalent(oldField, newField, exact: false));
                    hasGeneratedAttributeChange |= !ReturnTypesEquivalent(oldField, newField, exact: true);
                }
            }
            else if (oldSymbol is IMethodSymbol oldMethod && newSymbol is IMethodSymbol newMethod)
            {
                // Changing property accessor to auto-property accessor adds a field:
                if (oldMethod is { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol oldProperty } && !oldProperty.IsAutoProperty() &&
                    newMethod is { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol newProperty } && newProperty.IsAutoProperty() &&
                    !capabilities.Grant(GetRequiredAddFieldCapabilities(newMethod)))
                {
                    rudeEdit = RudeEditKind.InsertNotSupportedByRuntime;
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

                // VB implements clause (the method name is the same, but interface implementations differ)
                if (oldMethod.Name == newMethod.Name &&
                    !oldMethod.ExplicitInterfaceImplementations.SequenceEqual(newMethod.ExplicitInterfaceImplementations, SymbolsEquivalent))
                {
                    rudeEdit = RudeEditKind.ImplementsClauseUpdate;
                }

                // VB handles clause
                if (!AreHandledEventsEqual(oldMethod, newMethod))
                {
                    rudeEdit = RudeEditKind.HandlesClauseUpdate;
                }

                if (oldMethod.IsReadOnly != newMethod.IsReadOnly)
                {
                    hasGeneratedAttributeChange = true;
                }

                if (oldMethod.IsInitOnly != newMethod.IsInitOnly)
                {
                    // modreq(IsExternalInit) on the return type
                    rudeEdit = RudeEditKind.AccessorKindUpdate;
                }

                // Check return type - do not report for accessors, their containing symbol will report the rude edits and attribute updates.
                if (rudeEdit == RudeEditKind.None &&
                    oldMethod.AssociatedSymbol == null &&
                    newMethod.AssociatedSymbol == null &&
                    !IsMemberOrDelegateReplaced(oldMethod, newMethod))
                {
                    Debug.Assert(ReturnTypesEquivalent(oldMethod, newMethod, exact: false));
                    hasGeneratedReturnTypeAttributeChange |= !ReturnTypesEquivalent(oldMethod, newMethod, exact: true);
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
                        Debug.Assert(ReturnTypesEquivalent(oldType.DelegateInvokeMethod, newType.DelegateInvokeMethod, exact: false));

                        hasGeneratedReturnTypeAttributeChange |= !ReturnTypesEquivalent(oldType.DelegateInvokeMethod, newType.DelegateInvokeMethod, exact: true);
                    }
                }
            }
            else if (oldSymbol is IPropertySymbol oldProperty && newSymbol is IPropertySymbol newProperty)
            {
                if (!IsMemberOrDelegateReplaced(oldProperty, newProperty))
                {
                    Debug.Assert(ReturnTypesEquivalent(oldProperty, newProperty, exact: false));
                    hasGeneratedReturnTypeAttributeChange |= !ReturnTypesEquivalent(oldProperty, newProperty, exact: true);
                }
            }
            else if (oldSymbol is IEventSymbol oldEvent && newSymbol is IEventSymbol newEvent)
            {
                // "readonly" modifier can only be applied on the event itself, not on its accessors.
                if (oldEvent.AddMethod != null && newEvent.AddMethod != null && oldEvent.AddMethod.IsReadOnly != newEvent.AddMethod.IsReadOnly ||
                    oldEvent.RemoveMethod != null && newEvent.RemoveMethod != null && oldEvent.RemoveMethod.IsReadOnly != newEvent.RemoveMethod.IsReadOnly)
                {
                    hasGeneratedAttributeChange = true;
                }
                else if (!IsMemberOrDelegateReplaced(oldEvent, newEvent))
                {
                    Debug.Assert(ReturnTypesEquivalent(oldEvent, newEvent, exact: false));
                    hasGeneratedReturnTypeAttributeChange |= !ReturnTypesEquivalent(oldEvent, newEvent, exact: true);
                }
            }
            else if (oldSymbol is IParameterSymbol oldParameter && newSymbol is IParameterSymbol newParameter)
            {
                // If the containing member is being replaced then parameters are not being updated.
                if (!IsMemberOrDelegateReplaced(oldParameter.ContainingSymbol, newParameter.ContainingSymbol))
                {
                    if (IsExtensionMethodThisParameter(oldParameter) != IsExtensionMethodThisParameter(newParameter) ||
                        GeneratesParameterAttribute(oldParameter.RefKind) != GeneratesParameterAttribute(newParameter.RefKind) ||
                        oldParameter.IsParams != newParameter.IsParams ||
                        !ParameterTypesEquivalent(oldParameter, newParameter, exact: true))
                    {
                        hasGeneratedAttributeChange = true;
                    }

                    if (oldParameter.HasExplicitDefaultValue != newParameter.HasExplicitDefaultValue ||
                        oldParameter.HasExplicitDefaultValue && !Equals(oldParameter.ExplicitDefaultValue, newParameter.ExplicitDefaultValue))
                    {
                        rudeEdit = RudeEditKind.InitializerUpdate;
                    }
                    else if (oldParameter.Name != newParameter.Name && !capabilities.Grant(EditAndContinueCapabilities.UpdateParameters))
                    {
                        rudeEdit = RudeEditKind.RenamingNotSupportedByRuntime;
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
                InGenericContext(oldSymbol) &&
                !capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod))
            {
                rudeEdit = RudeEditKind.UpdatingGenericNotSupportedByRuntime;
            }

            if (rudeEdit != RudeEditKind.None)
            {
                diagnosticContext.Report(rudeEdit, cancellationToken);
            }
        }

        private static bool GeneratesParameterAttribute(RefKind kind)
            => kind is RefKind.In or RefKind.RefReadOnlyParameter;

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

        private static RudeEditKind GetSignatureChangeRudeEdit(ISymbol oldMember, ISymbol newMember, EditAndContinueCapabilitiesGrantor capabilities)
        {
            if (oldMember.Kind != newMember.Kind)
            {
                // rude edit will be reported later
                return RudeEditKind.None;
            }

            if (IsGlobalMain(oldMember))
            {
                // Only return type can be changed:
                Debug.Assert(ParameterTypesEquivalent(oldMember.GetParameters(), newMember.GetParameters(), exact: true));

                return RudeEditKind.ChangeImplicitMainReturnType;
            }

            if (!AllowsDeletion(newMember))
            {
                return RudeEditKind.TypeUpdate;
            }

            // Note: do not report a rude edit for property/event accessors as it will already be reported for the property/event itself.
            if (!CanRenameOrChangeSignature(oldMember, newMember, capabilities) &&
                oldMember is not IMethodSymbol { AssociatedSymbol.Kind: SymbolKind.Property or SymbolKind.Event })
            {
                return RudeEditKind.ChangingSignatureNotSupportedByRuntime;
            }

            return RudeEditKind.None;
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

        private static bool IsExtensionMethodThisParameter(IParameterSymbol parameter)
            => parameter is { Ordinal: 0, ContainingSymbol: IMethodSymbol { IsExtensionMethod: true } };

        private void AnalyzeSymbolUpdate(
            in DiagnosticContext diagnosticContext,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            out bool hasAttributeChange,
            CancellationToken cancellationToken)
        {
            // TODO: fails in VB on delegate parameter https://github.com/dotnet/roslyn/issues/53337
            // Contract.ThrowIfFalse(newSymbol.IsImplicitlyDeclared == newDeclaration is null);

            ReportUpdatedSymbolDeclarationRudeEdits(
                diagnosticContext, capabilities, out var hasGeneratedAttributeChange, out var hasGeneratedReturnTypeAttributeChange, cancellationToken);

            // We don't check capabilities of the runtime to update compiler generated attributes.
            // All runtimes support changing the attributes in metadata, some just don't reflect the changes in the Reflection model.
            // Having compiler-generated attributes visible via Reflaction API is not that important.
            ReportCustomAttributeRudeEdits(diagnosticContext, capabilities, out var hasSymbolAttributeChange, out var hasReturnTypeAttributeChange, cancellationToken);
            hasSymbolAttributeChange |= hasGeneratedAttributeChange;
            hasReturnTypeAttributeChange |= hasGeneratedReturnTypeAttributeChange;

            var oldSymbol = diagnosticContext.RequiredOldSymbol;
            var newSymbol = diagnosticContext.RequiredNewSymbol;

            if (oldSymbol is IParameterSymbol oldParameter && newSymbol is IParameterSymbol newParameter)
            {
                AddSemanticEditsOriginatingFromParameterUpdate(semanticEdits, oldParameter, newParameter, diagnosticContext.NewModel.Compilation, cancellationToken);

                // Attributes applied on parameters of a delegate are applied to both Invoke and BeginInvoke methods. So are the parameter names.
                if ((hasSymbolAttributeChange || oldParameter.Name != newParameter.Name) &&
                    newParameter.ContainingType is INamedTypeSymbol { TypeKind: TypeKind.Delegate } newContainingDelegateType)
                {
                    AddDelegateMethodEdit(semanticEdits, newContainingDelegateType, "Invoke", cancellationToken);
                    AddDelegateMethodEdit(semanticEdits, newContainingDelegateType, "BeginInvoke", cancellationToken);
                }
            }

            // Most symbol types will automatically have an edit added, so we just need to handle a few
            if (hasReturnTypeAttributeChange && newSymbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate } newDelegateType)
            {
                // attributes applied on return type of a delegate are applied to both Invoke and EndInvoke methods
                AddDelegateMethodEdit(semanticEdits, newDelegateType, "Invoke", cancellationToken);
                AddDelegateMethodEdit(semanticEdits, newDelegateType, "EndInvoke", cancellationToken);
            }

            hasAttributeChange = hasSymbolAttributeChange || hasReturnTypeAttributeChange;
        }

        /// <summary>
        /// Semantic edits of members synthesized based on parameters that have no declaring syntax (<see cref="GetSymbolDeclarationSyntax(ISymbol, CancellationToken)"/> returns null)
        /// and therefore not produced by <see cref="GetSymbolEdits(EditKind, SyntaxNode?, SyntaxNode?, SemanticModel?, SemanticModel, Match{SyntaxNode}, IReadOnlyDictionary{SyntaxNode, EditKind}, SymbolInfoCache, CancellationToken)"/>
        /// </summary>
        private void AddSemanticEditsOriginatingFromParameterUpdate(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IParameterSymbol oldParameterSymbol,
            IParameterSymbol newParameterSymbol,
            Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            var oldContainingMember = oldParameterSymbol.ContainingSymbol;
            var newContainingMember = newParameterSymbol.ContainingSymbol;

            if (oldContainingMember.ContainingType.IsRecord &&
                newContainingMember.ContainingType.IsRecord &&
                IsPrimaryConstructor(oldContainingMember, cancellationToken) is var oldIsPrimary &&
                IsPrimaryConstructor(newContainingMember, cancellationToken) is var newIsPrimary)
            {
                // both parameters are primary and differ in name or type
                if (oldIsPrimary && newIsPrimary && (oldParameterSymbol.Name != newParameterSymbol.Name || !ParameterTypesEquivalent(oldParameterSymbol, newParameterSymbol, exact: false)))
                {
                    var oldPrimaryConstructor = (IMethodSymbol)oldContainingMember;
                    var newPrimaryConstructor = (IMethodSymbol)newContainingMember;
                    var containingSymbolKey = SymbolKey.Create(oldContainingMember.ContainingSymbol, cancellationToken);

                    // Note: Edits for synthesized properties were already created by GetSymbolEdits.

                    // add delete and insert edits of synthesized deconstructor:
                    var oldSynthesizedDeconstructor = oldPrimaryConstructor.GetMatchingDeconstructor();
                    var newSynthesizedDeconstructor = newPrimaryConstructor.GetMatchingDeconstructor();
                    Contract.ThrowIfNull(oldSynthesizedDeconstructor);
                    Contract.ThrowIfNull(newSynthesizedDeconstructor);

                    AddMemberSignatureOrNameChangeEdits(semanticEdits, oldSynthesizedDeconstructor, newSynthesizedDeconstructor, containingSymbolKey, cancellationToken);

                    // add updates of synthesized methods:
                    AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newContainingMember.ContainingType, cancellationToken);
                }
            }
        }

        private static void AddDelegateMethodEdit(ArrayBuilder<SemanticEditInfo> semanticEdits, INamedTypeSymbol delegateType, string methodName, CancellationToken cancellationToken)
        {
            var beginInvokeMethod = delegateType.GetMembers(methodName).FirstOrDefault();
            if (beginInvokeMethod != null)
            {
                semanticEdits.Add(SemanticEditInfo.CreateUpdate(SymbolKey.Create(beginInvokeMethod, cancellationToken), syntaxMap: null, syntaxMapTree: null, partialType: null));
            }
        }

        private void ReportCustomAttributeRudeEdits(
            in DiagnosticContext diagnosticContext,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasAttributeChange,
            out bool hasReturnTypeAttributeChange,
            CancellationToken cancellationToken)
        {
            var oldSymbol = diagnosticContext.RequiredOldSymbol;
            var newSymbol = diagnosticContext.RequiredNewSymbol;

            // This is the only case we care about whether to issue an edit or not, because this is the only case where types have their attributes checked
            // and types are the only things that would otherwise not have edits reported.
            hasAttributeChange = ReportCustomAttributeRudeEdits(diagnosticContext, oldSymbol.GetAttributes(), newSymbol.GetAttributes(), capabilities, cancellationToken);

            hasReturnTypeAttributeChange = false;

            if (oldSymbol is IMethodSymbol oldMethod &&
                newSymbol is IMethodSymbol newMethod)
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnosticContext, oldMethod.GetReturnTypeAttributes(), newMethod.GetReturnTypeAttributes(), capabilities, cancellationToken);
            }
            else if (oldSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var oldInvokeMethod } &&
                     newSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var newInvokeMethod })
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnosticContext, oldInvokeMethod.GetReturnTypeAttributes(), newInvokeMethod.GetReturnTypeAttributes(), capabilities, cancellationToken);
            }
        }

        private bool ReportCustomAttributeRudeEdits(
            in DiagnosticContext diagnosticContext,
            ImmutableArray<AttributeData>? oldAttributes,
            ImmutableArray<AttributeData> newAttributes,
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
                diagnosticContext.Report(RudeEditKind.ChangingAttributesNotSupportedByRuntime, cancellationToken);
                return false;
            }

            var oldSymbol = diagnosticContext.RequiredOldSymbol;

            // Updating type parameter attributes is currently not supported.
            if (oldSymbol is ITypeParameterSymbol)
            {
                var rudeEdit = oldSymbol.ContainingSymbol.Kind == SymbolKind.Method ? RudeEditKind.GenericMethodUpdate : RudeEditKind.GenericTypeUpdate;
                diagnosticContext.Report(rudeEdit, cancellationToken);
                return false;
            }

            // Even if the runtime supports attribute changes, only attributes stored in the CustomAttributes table are editable
            foreach (var attributeData in changedAttributes)
            {
                if (IsNonCustomAttribute(attributeData))
                {
                    diagnosticContext.Report(RudeEditKind.ChangingNonCustomAttribute, cancellationToken, arguments: new[]
                    {
                        attributeData.AttributeClass!.Name,
                        GetDisplayKind(diagnosticContext.RequiredNewSymbol)
                    });

                    return false;
                }

                if (attributeData.AttributeClass is
                    {
                        Name: "InlineArrayAttribute",
                        ContainingNamespace.Name: "CompilerServices",
                        ContainingNamespace.ContainingNamespace.Name: "Runtime",
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: "System",
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
                    })
                {
                    diagnosticContext.Report(RudeEditKind.ChangingAttribute, cancellationToken, arguments: new[]
                    {
                        attributeData.AttributeClass.Name,
                    });

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
        private static bool CanRenameOrChangeSignature(ISymbol oldSymbol, ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities)
            => CanAddNewMemberToExistingType(newSymbol, capabilities) &&
               CanUpdateMemberBody(oldSymbol, capabilities);

        private static bool CanAddNewMemberToExistingType(ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities)
        {
            var requiredCapabilities = EditAndContinueCapabilities.None;

            if (newSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol)
            {
                requiredCapabilities |= GetRequiredAddMethodCapabilities(newSymbol);
            }

            if (newSymbol is IFieldSymbol || newSymbol.IsAutoProperty())
            {
                requiredCapabilities |= GetRequiredAddFieldCapabilities(newSymbol);
            }

            return capabilities.Grant(requiredCapabilities);
        }

        private static EditAndContinueCapabilities GetRequiredAddMethodCapabilities(ISymbol symbol)
            => EditAndContinueCapabilities.AddMethodToExistingType |
               (InGenericContext(symbol) ? EditAndContinueCapabilities.GenericAddMethodToExistingType : 0);

        private static EditAndContinueCapabilities GetRequiredAddFieldCapabilities(ISymbol symbol)
            => (symbol.IsStatic ? EditAndContinueCapabilities.AddStaticFieldToExistingType : EditAndContinueCapabilities.AddInstanceFieldToExistingType) |
               (InGenericContext(symbol) ? EditAndContinueCapabilities.GenericAddFieldToExistingType : 0);

        private static bool CanUpdateMemberBody(ISymbol oldSymbol, EditAndContinueCapabilitiesGrantor capabilities)
        {
            // If the new member is generic and the old one isn't then the old one will be updated (via delete edit) and the new one inserted.
            if (InGenericContext(oldSymbol))
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
                // We update all synthesized members regardless of whether the original change in the property actually changed them.
                // We could avoid these updates if we check the details (e.g. name & type matching, etc.)

                var symbolKey = SymbolKey.Create(member, cancellationToken);
                semanticEdits.Add(SemanticEditInfo.CreateUpdate(symbolKey, syntaxMap: null, syntaxMapTree: null, partialType: null));
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

        internal readonly struct DiagnosticContext(
            AbstractEditAndContinueAnalyzer analyzer,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol? oldSymbol,
            ISymbol? newSymbol,
            SyntaxNode? newNode,
            SemanticModel newModel,
            Match<SyntaxNode>? topMatch,
            TextSpan diagnosticSpan)
        {
            public SemanticModel NewModel => newModel;

            public ISymbol? OldSymbol
                => oldSymbol;

            public ISymbol RequiredOldSymbol
            {
                get
                {
                    Contract.ThrowIfNull(oldSymbol);
                    return oldSymbol;
                }
            }

            public ISymbol RequiredNewSymbol
            {
                get
                {
                    Contract.ThrowIfNull(newSymbol);
                    return newSymbol;
                }
            }

            private SyntaxNode GetDiagnosticNode(out int distance, CancellationToken cancellationToken)
            {
                distance = 0;

                if (newNode != null)
                {
                    return newNode;
                }

                var newDiagnosticSymbol = newSymbol;
                if (newDiagnosticSymbol == null)
                {
                    Debug.Assert(oldSymbol != null);

                    // try to resolve containing symbol:
                    newDiagnosticSymbol = TryGetNewContainer(oldSymbol, ref distance, cancellationToken);

                    // try to map container syntax:
                    if (newDiagnosticSymbol == null && topMatch != null)
                    {
                        var oldContainerDeclaration = analyzer.GetSymbolDeclarationSyntax(oldSymbol.ContainingSymbol, topMatch.OldRoot.SyntaxTree, cancellationToken);
                        if (oldContainerDeclaration != null &&
                            topMatch.TryGetNewNode(oldContainerDeclaration, out var newContainerDeclaration))
                        {
                            return newContainerDeclaration;
                        }
                    }
                }

                while (newDiagnosticSymbol != null)
                {
                    // TODO: condition !newDiagnosticSymbol.IsImplicitlyDeclared should not be needed https://github.com/dotnet/roslyn/issues/68510
                    if (newDiagnosticSymbol.DeclaringSyntaxReferences.Length > 0 && !newDiagnosticSymbol.IsImplicitlyDeclared)
                    {
                        var node = analyzer.GetSymbolDeclarationSyntax(newDiagnosticSymbol, newModel.SyntaxTree, cancellationToken);
                        if (node != null)
                        {
                            return node;
                        }
                    }

                    if (newDiagnosticSymbol.Kind is not (SymbolKind.Parameter or SymbolKind.TypeParameter))
                    {
                        distance++;
                    }

                    newDiagnosticSymbol = newDiagnosticSymbol.ContainingSymbol;
                }

                return newModel.SyntaxTree.GetRoot(cancellationToken);
            }

            private ISymbol? TryGetNewContainer(ISymbol oldSymbol, ref int distance, CancellationToken cancellationToken)
            {
                var oldContainer = oldSymbol.ContainingSymbol;

                if (oldSymbol.Kind is not (SymbolKind.Parameter or SymbolKind.TypeParameter))
                {
                    distance++;
                }

                while (oldContainer is not null and not INamespaceSymbol { IsGlobalNamespace: true })
                {
                    var symbolKey = SymbolKey.Create(oldSymbol, cancellationToken);
                    if (symbolKey.Resolve(newModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol is { } newSymbol)
                    {
                        return newSymbol;
                    }

                    oldContainer = oldContainer.ContainingSymbol;
                    distance++;
                }

                return null;
            }

            public void Report(RudeEditKind kind, TextSpan span)
                => diagnostics.Add(new RudeEditDiagnostic(kind, span));

            /// <summary>
            /// Reports rude edit in the context of newDeclaration.
            /// 
            /// If <paramref name="locationNode"/> is in the same syntax tree as newDeclaration its span will be used for the location of the diagnostic, otherwise the diagnostic will be reported on the newDeclaration.
            /// If <paramref name="arguments"/> is given it is used for the diagnostic arguments, otherwise the display name of the newDeclaration is passed as the single argument.
            /// 
            /// The rude edit will be associated with the syntax kind of newDeclaration in telemetry.
            /// </summary>
            public void Report(RudeEditKind kind, SyntaxNode locationNode, CancellationToken cancellationToken, string?[]? arguments = null)
                => Report(
                    kind,
                    cancellationToken,
                    span: (locationNode.SyntaxTree == newModel.SyntaxTree) ?
                        locationNode.Span : analyzer.GetDiagnosticSpan(GetDiagnosticNode(out var distance, cancellationToken), distance > 0 ? EditKind.Delete : EditKind.Update),
                    arguments);

            /// <summary>
            /// Reports rude edit in the context of newDeclaration.
            /// 
            /// If <paramref name="span"/> is given it will be used for the location of the diagnostic, otherwise the diagnostic will be reported on the newDeclaration.
            /// If <paramref name="arguments"/> is given it is used for the diagnostic arguments, otherwise the display name of the newDeclaration is passed as the single argument.
            /// 
            /// The rude edit will be associated with the syntax kind of newDeclaration in telemetry.
            /// </summary>
            public void Report(RudeEditKind kind, CancellationToken cancellationToken, TextSpan? span = null, string?[]? arguments = null)
            {
                var node = GetDiagnosticNode(out var distance, cancellationToken);

                span ??= diagnosticSpan.IsEmpty
                    ? analyzer.GetDiagnosticSpan(node, (distance > 0 || kind == RudeEditKind.ChangeImplicitMainReturnType) ? EditKind.Delete : EditKind.Update)
                    : diagnosticSpan;

                diagnostics.Add(new RudeEditDiagnostic(
                    kind,
                    span.Value,
                    node,
                    arguments ?? kind switch
                    {
                        RudeEditKind.TypeKindUpdate or
                        RudeEditKind.ChangeImplicitMainReturnType or
                        RudeEditKind.GenericMethodUpdate or
                        RudeEditKind.GenericTypeUpdate or
                        RudeEditKind.SwitchBetweenLambdaAndLocalFunction or
                        RudeEditKind.AccessorKindUpdate or
                        RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas
                            => Array.Empty<string>(),

                        RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime
                            => new[] { CreateNewOnMetadataUpdateAttributeName },

                        RudeEditKind.Renamed
                            => new[] { analyzer.GetDisplayKindAndName(oldSymbol!, fullyQualify: false) },

                        _ => new[]
                        {
                            // Use name of oldSymbol, in case the symbol we are refering to has been renamed:
                            ((oldSymbol ?? newSymbol) is not { } symbol)
                                ? analyzer.GetDisplayName(node)
                                // Include member name if it is deleted or implicitly declared, otherwise it might not be obvious which member is being referred to.
                                : distance > 0
                                    ? analyzer.GetDisplayKindAndName(symbol, fullyQualify: distance > 1)
                                    : analyzer.GetDisplayKind(symbol)
                        }
                    }));
            }

            public void ReportTypeLayoutUpdateRudeEdits(CancellationToken cancellationToken)
            {
                Debug.Assert(newSymbol != null);

                Report(
                    (newSymbol.ContainingType.TypeKind == TypeKind.Struct) ? RudeEditKind.InsertIntoStruct : RudeEditKind.InsertIntoClassWithLayout,
                    cancellationToken,
                    arguments: new[]
                    {
                        analyzer.GetDisplayKind(newSymbol),
                        analyzer.GetDisplayKind(newSymbol.ContainingType)
                    });
            }

            public DiagnosticContext WithSymbols(ISymbol oldSymbol, ISymbol newSymbol)
                => new(analyzer, diagnostics, oldSymbol, newSymbol, newNode, newModel, topMatch, diagnosticSpan);
        }

        private DiagnosticContext CreateDiagnosticContext(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol? oldSymbol, ISymbol? newSymbol, SyntaxNode? newNode, SemanticModel newModel, Match<SyntaxNode>? topMatch, TextSpan diagnosticSpan = default)
            => new(this, diagnostics, oldSymbol, newSymbol, newNode, newModel, topMatch, diagnosticSpan);

        #region Type Layout Update Validation 

        internal void ReportTypeLayoutUpdateRudeEdits(in DiagnosticContext diagnosticContext, ISymbol newSymbol, CancellationToken cancellationToken)
        {
            switch (newSymbol.Kind)
            {
                case SymbolKind.Field:
                    if (HasExplicitOrSequentialLayout(newSymbol.ContainingType, diagnosticContext.NewModel))
                    {
                        diagnosticContext.ReportTypeLayoutUpdateRudeEdits(cancellationToken);
                    }

                    break;

                case SymbolKind.Property:
                    if (newSymbol.IsAutoProperty() &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, diagnosticContext.NewModel))
                    {
                        diagnosticContext.ReportTypeLayoutUpdateRudeEdits(cancellationToken);
                    }

                    break;

                case SymbolKind.Event:
                    if (HasBackingField((IEventSymbol)newSymbol) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, diagnosticContext.NewModel))
                    {
                        diagnosticContext.ReportTypeLayoutUpdateRudeEdits(cancellationToken);
                    }

                    break;

                case SymbolKind.Parameter:
                    // parameter of a primary constructor that's lifted to a field
                    if (HasBackingField((IParameterSymbol)newSymbol, cancellationToken) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, diagnosticContext.NewModel))
                    {
                        diagnosticContext.ReportTypeLayoutUpdateRudeEdits(cancellationToken);
                    }

                    break;
            }
        }

        private bool HasBackingField(IParameterSymbol parameter, CancellationToken cancellationToken)
            => IsPrimaryConstructor(parameter.ContainingSymbol, cancellationToken) &&
               parameter.ContainingType.GetMembers($"<{parameter.Name}>P").Any(m => m.Kind == SymbolKind.Field);

        private static bool HasBackingField(IEventSymbol @event)
        {
#nullable disable // https://github.com/dotnet/roslyn/issues/39288
            return @event.AddMethod.IsImplicitlyDeclared
#nullable enable
                && !@event.IsAbstract;
        }

        private static bool HasExplicitOrSequentialLayout(INamedTypeSymbol type, SemanticModel model)
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

            var layoutAttribute = model.Compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName!);
            if (layoutAttribute == null)
            {
                return false;
            }

            foreach (var attribute in attributes)
            {
                RoslynDebug.Assert(attribute.AttributeClass is object);
                if (attribute.AttributeClass.Equals(layoutAttribute) && attribute.ConstructorArguments.Length == 1)
                {
                    return attribute.ConstructorArguments.Single().Value switch
                    {
                        int value => value != (int)LayoutKind.Auto,
                        short value => value != (short)LayoutKind.Auto,
                        _ => false
                    };
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
                if (!TryFindMemberDeclaration(root: null, newNode, newNode.Span, out var newDeclarations))
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
                        var oldBody = TryGetDeclarationBody(oldDeclaration, symbol: null);
                        var newBody = TryGetDeclarationBody(newDeclaration, symbol: null);

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
                var isPartialEdit = IsPartialTypeEdit(oldType, newType, oldSyntaxTree, newSyntaxTree);
                var typeKey = SymbolKey.Create(newType, cancellationToken);
                var partialType = isPartialEdit ? typeKey : (SymbolKey?)null;
                var syntaxMapTree = isPartialEdit ? newSyntaxTree : null;

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

                var memberInitializerContainingLambdaReported = false;

                // We might have already reported rude edits for initializers that have been updated.
                // It would be possible to track those as well but not worth the added complexity.
                var unsupportedOperationReported = false;

                foreach (var newCtor in isStatic ? newType.StaticConstructors : newType.InstanceConstructors)
                {
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

                    SyntaxNode? oldDeclaration = null;
                    SyntaxNode? newDeclaration = null;
                    IMethodSymbol? oldCtor;
                    bool hasSignatureChanges;

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

                        if (topMatch.TryGetOldNode(newDeclaration, out oldDeclaration))
                        {
                            Contract.ThrowIfNull(oldModel);

                            oldCtor = (IMethodSymbol)GetRequiredDeclaredSymbol(oldModel, oldDeclaration, cancellationToken);
                            Contract.ThrowIfFalse(oldCtor is { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor });

                            hasSignatureChanges = !MemberOrDelegateSignaturesEquivalent(oldCtor, newCtor, exact: false);
                        }
                        else if (newCtor.Parameters.Length == 0)
                        {
                            oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                            hasSignatureChanges = false;
                        }
                        else
                        {
                            var resolution = newCtorKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken);

                            // There may be semantic errors in the compilation that result in multiple candidates.
                            // Pick the first candidate.

                            oldCtor = (IMethodSymbol?)resolution.Symbol;

                            // SymbolKey-resolved constructors have the same signatures.
                            Debug.Assert(oldCtor == null || MemberOrDelegateSignaturesEquivalent(oldCtor, newCtor, exact: false));
                            hasSignatureChanges = false;
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

                        hasSignatureChanges = false;
                    }

                    var diagnosticContext = CreateDiagnosticContext(diagnostics, oldCtor, newCtor, newDeclaration, newModel, topMatch);

                    // Report an error if the updated constructor's declaration is in the current document 
                    // and its body edit is disallowed (e.g. the body itself or any member initializer contains stackalloc).
                    // If the declaration represents a primary constructor the body will be null.
                    if (newDeclaration?.SyntaxTree == newSyntaxTree)
                    {
                        unsupportedOperationReported |=
                            TryGetDeclarationBody(newDeclaration, newCtor) is { } newBody && ReportUnsupportedOperations(diagnosticContext, newBody, cancellationToken) ||
                            oldDeclaration != null && TryGetDeclarationBody(oldDeclaration, oldCtor) is { } oldBody && ReportUnsupportedOperations(diagnosticContext, oldBody, cancellationToken);
                    }

                    if (oldCtor != null)
                    {
                        // We don't need to check initializers of the new type since any change that would
                        // add stackalloc or other disallowed syntax would already be reported as rude edit.
                        unsupportedOperationReported |= AnyMemberInitializerBody(
                            oldType,
                            body => ReportUnsupportedOperations(diagnosticContext, body, cancellationToken),
                            isStatic,
                            cancellationToken);

                        if (hasSignatureChanges)
                        {
                            // Even though we can't remap active statements between the deleted and inserted methods,
                            // we still need syntax map to map lambdas.
                            AddMemberSignatureOrNameChangeEdits(semanticEdits, oldCtor, newCtor, typeKey, cancellationToken);
                        }
                        else
                        {
                            semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, newCtorKey, syntaxMapToUse, syntaxMapTree, partialType, deletedSymbolContainer: null));
                        }
                    }
                    else
                    {
                        if (!memberInitializerContainingLambdaReported &&
                            AnyMemberInitializerBody(oldType, ContainsLambda, isStatic, cancellationToken))
                        {
                            // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                            // rude edit: Adding a constructor to a type with a field or property initializer that contains an anonymous function
                            diagnosticContext.Report(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, cancellationToken);
                            memberInitializerContainingLambdaReported = true;
                            continue;
                        }

                        semanticEdits.Add(SemanticEditInfo.CreateInsert(newCtorKey, partialType));
                    }

                    // primary record constructor updated to non-primary and vice versa:
                    if (newType.IsRecord)
                    {
                        var oldCtorIsPrimary = oldCtor != null && IsPrimaryConstructor(oldCtor, cancellationToken);
                        var newCtorIsPrimary = IsPrimaryConstructor(newCtor, cancellationToken);

                        if (hasSignatureChanges && oldCtorIsPrimary && newCtorIsPrimary ||
                            oldCtorIsPrimary != newCtorIsPrimary)
                        {
                            // Deconstructor:
                            AddDeconstructorEdits(semanticEdits, oldCtor, newCtor, typeKey, oldCompilation, newModel.Compilation, isParameterDelete: newCtorIsPrimary, cancellationToken);

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
                        semanticEdits.Add(SemanticEditInfo.CreateDelete(
                            SymbolKey.Create(oldType.InstanceConstructors.Single(c => c.Parameters is []), cancellationToken),
                            deletedSymbolContainer: typeKey,
                            partialType));
                    }

                    // Removing the last instance constructor with parameters inserts synthesized default constructor.
                    // We don't need to add an explicit semantic edit for synthesized members though since the compiler
                    // emits them automatically.
                }
            }
        }

        /// <summary>
        /// Return true if <paramref name="predicate"/> is true for a body of any instance/static member of <paramref name="type"/> that has an initializer.
        /// </summary>
        private bool AnyMemberInitializerBody(INamedTypeSymbol type, Func<MemberBody, bool> predicate, bool isStatic, CancellationToken cancellationToken)
        {
            // checking the old type for existing lambdas (it's ok for the new initializers to contain lambdas)

            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic == isStatic &&
                    (member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property) &&
                    member.DeclaringSyntaxReferences.Length > 0) // skip generated fields (e.g. VB auto-property backing fields)
                {
                    var syntax = GetSymbolDeclarationSyntax(member, cancellationToken);
                    if (IsDeclarationWithInitializer(syntax) && TryGetDeclarationBody(syntax, member) is { } body && predicate(body))
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

        private static bool IsPartialTypeEdit(ISymbol? oldSymbol, ISymbol? newSymbol, SyntaxTree oldSyntaxTree, SyntaxTree newSyntaxTree)
        {
            // If any of the partial declarations of the new or the old type are in another document
            // the edit will need to be merged with other partial edits with matching partial type
            static bool IsNotInDocument(SyntaxReference reference, SyntaxTree syntaxTree)
                => reference.SyntaxTree != syntaxTree;

            static bool IsPartialTypeEdit(ISymbol? symbol, SyntaxTree tree)
                => symbol is INamedTypeSymbol &&
                   symbol.DeclaringSyntaxReferences.Length > 1 && symbol.DeclaringSyntaxReferences.Any(IsNotInDocument, tree);

            return IsPartialTypeEdit(oldSymbol, oldSyntaxTree) ||
                   IsPartialTypeEdit(newSymbol, newSyntaxTree);
        }

        #endregion

        #region Lambdas and Closures

        private void ReportLambdaAndClosureRudeEdits(
            SemanticModel? oldModel,
            ISymbol oldMember,
            MemberBody? oldMemberBody,
            SyntaxNode? oldDeclaration,
            SemanticModel newModel,
            ISymbol newMember,
            MemberBody? newMemberBody,
            SyntaxNode? newDeclaration,
            IReadOnlyDictionary<LambdaBody, LambdaInfo>? activeOrMatchedLambdas,
            BidirectionalMap<SyntaxNode> map,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool syntaxMapRequired,
            CancellationToken cancellationToken)
        {
            syntaxMapRequired = false;

            if (activeOrMatchedLambdas != null)
            {
                var anySignatureErrors = false;
                var hasUnmatchedLambdas = false;
                foreach (var (oldLambdaBody, newLambdaInfo) in activeOrMatchedLambdas)
                {
                    var newLambdaBody = newLambdaInfo.NewBody;
                    if (newLambdaBody == null)
                    {
                        hasUnmatchedLambdas = true;
                        continue;
                    }

                    var lambdaBodyMatch = newLambdaInfo.Match;
                    Debug.Assert(lambdaBodyMatch != null);

                    Debug.Assert(oldModel != null);

                    var oldLambda = oldLambdaBody.GetLambda();
                    var newLambda = newLambdaBody.GetLambda();

                    Debug.Assert(IsNestedFunction(newLambda) == IsNestedFunction(oldLambda));
                    var isNestedFunction = IsNestedFunction(newLambda);

                    var oldLambdaSymbol = isNestedFunction ? GetLambdaExpressionSymbol(oldModel, oldLambda, cancellationToken) : null;
                    var newLambdaSymbol = isNestedFunction ? GetLambdaExpressionSymbol(newModel, newLambda, cancellationToken) : null;

                    var diagnosticContext = CreateDiagnosticContext(diagnostics, oldLambdaSymbol, newLambdaSymbol, newLambda, newModel, topMatch: null);

                    var oldStateMachineInfo = oldLambdaBody.GetStateMachineInfo();
                    var newStateMachineInfo = newLambdaBody.GetStateMachineInfo();

                    ReportStateMachineBodyUpdateRudeEdits(diagnosticContext, lambdaBodyMatch.Value, oldStateMachineInfo, newStateMachineInfo, newLambdaInfo.HasActiveStatement, cancellationToken);

                    // When the delta IL of the containing method is emitted lambdas declared in it are also emitted.
                    // If the runtime does not support changing IL of the method (e.g. method containing stackalloc)
                    // we need to report a rude edit.
                    // If only trivia change the IL is going to be unchanged and only sequence points in the PDB change,
                    // so we do not report rude edits.

                    if (!oldLambdaBody.IsSyntaxEquivalentTo(newLambdaBody))
                    {
                        ReportMemberOrLambdaBodyUpdateRudeEdits(
                            diagnosticContext,
                            oldModel.Compilation,
                            oldLambda,
                            oldMember,
                            oldMemberBody,
                            oldLambdaBody,
                            newLambda,
                            newMember,
                            newMemberBody,
                            newLambdaBody,
                            capabilities,
                            oldStateMachineInfo,
                            newStateMachineInfo,
                            cancellationToken);

                        if ((IsGenericLocalFunction(oldLambda) || IsGenericLocalFunction(newLambda)) &&
                            !capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod))
                        {
                            diagnosticContext.Report(RudeEditKind.UpdatingGenericNotSupportedByRuntime, cancellationToken);
                        }
                    }

                    // query signatures are analyzed separately:
                    if (isNestedFunction)
                    {
                        ReportLambdaSignatureRudeEdits(diagnosticContext, oldLambda, newLambda, capabilities, out var hasErrors, cancellationToken);
                        anySignatureErrors |= hasErrors;
                    }
                }

                // Any unmatched lambdas would have contained an active statement and a rude edit would be reported in syntax analysis phase.
                // Skip the rest of lambda and closure analysis if such lambdas are present.
                if (hasUnmatchedLambdas)
                {
                    return;
                }

                ArrayBuilder<SyntaxNode>? lazyNewErroneousClauses = null;
                foreach (var (oldQueryClause, newQueryClause) in map.Forward)
                {
                    Debug.Assert(oldModel != null);

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

            var oldPrimaryConstructor = oldDeclaration != null && IsPrimaryConstructorDeclaration(oldDeclaration)
                ? (IMethodSymbol)oldMember
                : GetPrimaryConstructor(oldMember.ContainingType, cancellationToken);

            var newPrimaryConstructor = newDeclaration != null && IsPrimaryConstructorDeclaration(newDeclaration)
                ? (IMethodSymbol)newMember
                : GetPrimaryConstructor(newMember.ContainingType, cancellationToken);

            // If type layout is changed another rude edit is reported, so we can assume the layouts match.
            // We don't need to analyze primary parameter captures unless type layout disallows captures.
            var typeLayoutDisallowsNewCaptures =
                (newPrimaryConstructor != null || oldPrimaryConstructor != null) && HasExplicitOrSequentialLayout(newMember.ContainingType, newModel);

            // The primary constructor if its parameters are lifted into fields when accessed from this member, otherwise null.
            var oldLiftingPrimaryConstructor = oldMember != oldPrimaryConstructor && oldDeclaration != null && !IsDeclarationWithInitializer(oldDeclaration) ? oldPrimaryConstructor : null;
            var newLiftingPrimaryConstructor = newMember != newPrimaryConstructor && newDeclaration != null && !IsDeclarationWithInitializer(newDeclaration) ? newPrimaryConstructor : null;

            GetCapturedVariables(
                oldMemberBody,
                oldModel,
                oldLiftingPrimaryConstructor,
                ignorePrimaryParameterCaptures: !typeLayoutDisallowsNewCaptures,
                out var oldHasLambdasOrLocalFunctions,
                out var oldInLambdaCaptures,
                out var oldPrimaryCaptures);

            GetCapturedVariables(
                newMemberBody,
                newModel,
                newLiftingPrimaryConstructor,
                ignorePrimaryParameterCaptures: !typeLayoutDisallowsNewCaptures,
                out var newHasLambdasOrLocalFunctions,
                out var newInLambdaCaptures,
                out var newPrimaryCaptures);

            // Analyze primary parameter captures:

            ReportPrimaryParameterCaptureRudeEdits(
                diagnostics,
                oldLiftingPrimaryConstructor,
                oldPrimaryCaptures,
                newLiftingPrimaryConstructor,
                newPrimaryCaptures,
                newMember,
                cancellationToken);

            // Analyze captures in lambda bodies:

            if (!oldHasLambdasOrLocalFunctions && !newHasLambdasOrLocalFunctions)
            {
                return;
            }

            syntaxMapRequired = newHasLambdasOrLocalFunctions;

            // { new capture index -> old capture index }
            using var _1 = ArrayBuilder<int>.GetInstance(newInLambdaCaptures.Length, fillWithValue: 0, out var reverseCapturesMap);

            // { new capture index -> new closure scope or null for "this" }
            using var _2 = ArrayBuilder<SyntaxNode?>.GetInstance(newInLambdaCaptures.Length, fillWithValue: null, out var newCapturesToClosureScopes);

            // Can be calculated from other maps but it's simpler to just calculate it upfront.
            // { old capture index -> old closure scope or null for "this" }
            using var _3 = ArrayBuilder<SyntaxNode?>.GetInstance(oldInLambdaCaptures.Length, fillWithValue: null, out var oldCapturesToClosureScopes);

            CalculateCapturedVariablesMaps(
                oldInLambdaCaptures,
                oldDeclaration,
                oldPrimaryConstructor,
                newInLambdaCaptures,
                newMember,
                newDeclaration,
                newPrimaryConstructor,
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

            using var _11 = PooledDictionary<VariableCapture, int>.GetInstance(out var oldCapturesIndex);
            using var _12 = PooledDictionary<VariableCapture, int>.GetInstance(out var newCapturesIndex);

            BuildIndex(oldCapturesIndex, oldInLambdaCaptures);
            BuildIndex(newCapturesIndex, newInLambdaCaptures);

            if (activeOrMatchedLambdas != null)
            {
                var mappedLambdasHaveErrors = false;
                foreach (var (oldLambdaBody, newLambdaInfo) in activeOrMatchedLambdas)
                {
                    var newLambdaBody = newLambdaInfo.NewBody;

                    // The map now contains only matched lambdas. Any unmatched ones would have contained an active statement and 
                    // a rude edit would be reported in syntax analysis phase.
                    Debug.Assert(newLambdaInfo.Match != null && newLambdaBody != null);
                    Debug.Assert(oldModel != null);

                    var accessedOldCaptures = GetAccessedCaptures(oldLambdaBody, oldModel, oldInLambdaCaptures, oldCapturesIndex, oldLiftingPrimaryConstructor);
                    var accessedNewCaptures = GetAccessedCaptures(newLambdaBody, newModel, newInLambdaCaptures, newCapturesIndex, newLiftingPrimaryConstructor);

                    // Requirement: 
                    // (new(ReadInside) \/ new(WrittenInside)) /\ new(Captured) == (old(ReadInside) \/ old(WrittenInside)) /\ old(Captured)
                    for (var newCaptureIndex = 0; newCaptureIndex < newInLambdaCaptures.Length; newCaptureIndex++)
                    {
                        var newAccessed = accessedNewCaptures[newCaptureIndex];
                        var oldAccessed = accessedOldCaptures[reverseCapturesMap[newCaptureIndex]];

                        if (newAccessed != oldAccessed)
                        {
                            var newCapture = newInLambdaCaptures[newCaptureIndex];

                            var rudeEdit = newAccessed ? RudeEditKind.AccessingCapturedVariableInLambda : RudeEditKind.NotAccessingCapturedVariableInLambda;
                            var arguments = new[] { newCapture.Name, GetDisplayName(newLambdaBody.GetLambda()) };

                            if (newCapture.IsThis || oldAccessed)
                            {
                                // changed accessed to "this", or captured variable accessed in old lambda is not accessed in the new lambda
                                diagnostics.Add(new RudeEditDiagnostic(rudeEdit, GetDiagnosticSpan(newLambdaBody.GetLambda(), EditKind.Update), null, arguments));
                            }
                            else if (newAccessed)
                            {
                                // captured variable accessed in new lambda is not accessed in the old lambda
                                var hasUseSites = false;
                                foreach (var useSite in GetVariableUseSites(newLambdaBody.GetExpressionsAndStatements(), newCapture.Symbol, newModel, cancellationToken))
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

            foreach (var (oldLambda, oldLambdaBody1, oldLambdaBody2) in GetLambdaBodies(oldMemberBody))
            {
                oldHasLambdas |= !IsLocalFunction(oldLambda);

                if (!map.Forward.ContainsKey(oldLambda))
                {
                    Debug.Assert(oldModel != null);

                    ReportMultiScopeCaptures(oldLambdaBody1, oldModel, oldInLambdaCaptures, newInLambdaCaptures, oldCapturesToClosureScopes, oldCapturesIndex, oldLiftingPrimaryConstructor, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);

                    if (oldLambdaBody2 != null)
                    {
                        ReportMultiScopeCaptures(oldLambdaBody2, oldModel, oldInLambdaCaptures, newInLambdaCaptures, oldCapturesToClosureScopes, oldCapturesIndex, oldLiftingPrimaryConstructor, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);
                    }
                }
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
            var isInInterface = newMember.ContainingType.TypeKind == TypeKind.Interface;
            var isNewMemberInGenericContext = InGenericContext(newMember);

            foreach (var (newLambda, newLambdaBody1, newLambdaBody2) in GetLambdaBodies(newMemberBody))
            {
                if (!map.Reverse.ContainsKey(newLambda))
                {
                    if (!CanAddNewLambda(newLambda, newLambdaBody1, newLambdaBody2))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertNotSupportedByRuntime, GetDiagnosticSpan(newLambda, EditKind.Insert), newLambda, new string[] { GetDisplayName(newLambda, EditKind.Insert) }));
                    }

                    // TODO: https://github.com/dotnet/roslyn/issues/37128
                    // Local functions are emitted directly to the type containing the containing method.
                    // Although local functions are non-virtual the Core CLR currently does not support adding any method to an interface.
                    if (isInInterface && IsLocalFunction(newLambda))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod, GetDiagnosticSpan(newLambda, EditKind.Insert), newLambda, new string[] { GetDisplayName(newLambda, EditKind.Insert) }));
                    }

                    ReportMultiScopeCaptures(newLambdaBody1, newModel, newInLambdaCaptures, newInLambdaCaptures, newCapturesToClosureScopes, newCapturesIndex, newLiftingPrimaryConstructor, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);

                    if (newLambdaBody2 != null)
                    {
                        ReportMultiScopeCaptures(newLambdaBody2, newModel, newInLambdaCaptures, newInLambdaCaptures, newCapturesToClosureScopes, newCapturesIndex, newLiftingPrimaryConstructor, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);
                    }
                }
            }

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

                var inGenericLocalContext = newMemberBody != null && InGenericLocalContext(newLambda, newMemberBody.RootNodes);

                if (isNewMemberInGenericContext || inGenericLocalContext)
                {
                    requiredCapabilities |= EditAndContinueCapabilities.GenericAddMethodToExistingType;
                }

                // Static lambdas are cached in static fields, unless in generic local functions.
                // If either body is static we need to require the capabilities.
                var isLambdaCachedInField =
                    !inGenericLocalContext &&
                    !isLocalFunction &&
                    (GetAccessedCaptures(newLambdaBody1, newModel, newInLambdaCaptures, newCapturesIndex, newLiftingPrimaryConstructor).Equals(BitVector.Empty) ||
                        newLambdaBody2 != null && GetAccessedCaptures(newLambdaBody2, newModel, newInLambdaCaptures, newCapturesIndex, newLiftingPrimaryConstructor).Equals(BitVector.Empty));

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

        private IEnumerable<(SyntaxNode lambda, LambdaBody lambdaBody1, LambdaBody? lambdaBody2)> GetLambdaBodies(MemberBody? body)
        {
            if (body == null)
            {
                yield break;
            }

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

        private enum VariableCaptureKind
        {
            This,
            LocalOrParameter,
        }

        /// <summary>
        /// Represents a captured local variable or a parameter of the current member.
        /// Primary constructor parameters that are accessed via "this" are represented as
        /// <see cref="VariableCaptureKind.This"/>.
        /// 
        /// Equality ignores <paramref name="Symbol"/> if <paramref name="Kind"/> is <see cref="VariableCaptureKind.This"/>.
        /// </summary>
        private readonly record struct VariableCapture(VariableCaptureKind Kind, ISymbol Symbol)
        {
            public bool IsThis => Kind == VariableCaptureKind.This;
            public string Name => Symbol.Name;

            public bool Equals(VariableCapture other)
                => Kind == other.Kind && (Kind == VariableCaptureKind.This || ReferenceEquals(Symbol, other.Symbol));

            public override int GetHashCode()
                => Hash.Combine((int)Kind, (Kind == VariableCaptureKind.This) ? 0 : Symbol.GetHashCode());

            public static VariableCapture Create(ISymbol variable, IMethodSymbol? liftingPrimaryConstructor)
                => new(GetCaptureKind(variable, liftingPrimaryConstructor), variable);
        }

        private static VariableCaptureKind GetCaptureKind(ISymbol variable, IMethodSymbol? liftingPrimaryConstructor)
            => variable is IParameterSymbol parameter && (parameter.IsThis || parameter.ContainingSymbol == liftingPrimaryConstructor)
               ? VariableCaptureKind.This : VariableCaptureKind.LocalOrParameter;

        private void GetCapturedVariables(
            MemberBody? memberBody,
            SemanticModel? model,
            IMethodSymbol? liftingPrimaryConstructor,
            bool ignorePrimaryParameterCaptures,
            out bool hasLambdaBodies,
            out ImmutableArray<VariableCapture> variablesCapturedInLambdas,
            out ImmutableArray<IParameterSymbol> primaryParametersCapturedViaThis)
        {
            hasLambdaBodies = false;

            if (memberBody == null)
            {
                variablesCapturedInLambdas = ImmutableArray<VariableCapture>.Empty;
                primaryParametersCapturedViaThis = ImmutableArray<IParameterSymbol>.Empty;
                return;
            }

            Debug.Assert(model != null);

            PooledHashSet<VariableCapture>? inLambdaCapturesSet = null;
            ArrayBuilder<VariableCapture>? inLambdaCaptures = null;

            foreach (var (lambda, lambdaBody1, lambdaBody2) in GetLambdaBodies(memberBody))
            {
                hasLambdaBodies = true;

                AddCaptures(lambdaBody1);
                if (lambdaBody2 != null)
                {
                    AddCaptures(lambdaBody2);
                }

                void AddCaptures(LambdaBody lambdaBody)
                {
                    var captures = lambdaBody.GetCapturedVariables(model);
                    if (!captures.IsEmpty)
                    {
                        inLambdaCapturesSet ??= PooledHashSet<VariableCapture>.GetInstance();
                        inLambdaCaptures ??= ArrayBuilder<VariableCapture>.GetInstance();

                        foreach (var capture in captures)
                        {
                            var variableCapture = VariableCapture.Create(capture, liftingPrimaryConstructor);
                            if (inLambdaCapturesSet.Add(variableCapture))
                            {
                                inLambdaCaptures.Add(variableCapture);
                            }
                        }
                    }
                }
            }

            variablesCapturedInLambdas = inLambdaCaptures.ToImmutableOrEmptyAndFree();

            // only primary constructor parameters can be captured outside of lambda bodies:
            if (liftingPrimaryConstructor != null && !ignorePrimaryParameterCaptures)
            {
                primaryParametersCapturedViaThis = memberBody.GetCapturedVariables(model).SelectAsArray(
                    predicate: (capture, liftingPrimaryConstructor) => capture.ContainingSymbol == liftingPrimaryConstructor,
                    selector: (capture, _) => (IParameterSymbol)capture,
                    liftingPrimaryConstructor);
            }
            else
            {
                primaryParametersCapturedViaThis = ImmutableArray<IParameterSymbol>.Empty;
            }

            inLambdaCapturesSet?.Free();
        }

        private void ReportPrimaryParameterCaptureRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            IMethodSymbol? oldLiftingPrimaryConstructor,
            ImmutableArray<IParameterSymbol> oldPrimaryCaptures,
            IMethodSymbol? newLiftingPrimaryConstructor,
            ImmutableArray<IParameterSymbol> newPrimaryCaptures,
            ISymbol newMember,
            CancellationToken cancellationToken)
        {
            foreach (var newCapture in newPrimaryCaptures)
            {
                if (oldLiftingPrimaryConstructor == null || !IsCapturedPrimaryParameterCapturedInType(newCapture, oldLiftingPrimaryConstructor.ContainingType))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.CapturingPrimaryConstructorParameter,
                        GetSymbolLocationSpan(newCapture, cancellationToken),
                        node: null,
                        new[] { GetLayoutKindDisplay(newCapture), newCapture.Name }));
                }
            }

            // Disallow uncapturing primary parameters. We could allow it but would need to be sure the compiler is going to reuse
            // the backing field if the parameter is later captured again, rather then emitting a new one of the same name.

            foreach (var oldCapture in oldPrimaryCaptures)
            {
                if (newLiftingPrimaryConstructor == null || !IsCapturedPrimaryParameterCapturedInType(oldCapture, newLiftingPrimaryConstructor.ContainingType))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.NotCapturingPrimaryConstructorParameter,
                        GetSymbolLocationSpan(newMember, cancellationToken),
                        node: null,
                        new[] { GetLayoutKindDisplay(oldCapture), oldCapture.Name }));
                }
            }

            static bool IsCapturedPrimaryParameterCapturedInType(IParameterSymbol capture, INamedTypeSymbol otherType)
            {
                var oldBackingField = capture.GetPrimaryParameterBackingField();

                // captured parameter must have a backing field:
                Contract.ThrowIfNull(oldBackingField);

                // The backing field still exists in the new type:
                return otherType.GetMembers(oldBackingField.Name).Any();
            }

            static string GetLayoutKindDisplay(IParameterSymbol parameter)
                => (parameter.ContainingType.TypeKind == TypeKind.Struct) ? FeaturesResources.struct_ : FeaturesResources.class_with_explicit_or_sequential_layout;
        }

        private void ReportMultiScopeCaptures(
            LambdaBody lambdaBody,
            SemanticModel model,
            ImmutableArray<VariableCapture> captures,
            ImmutableArray<VariableCapture> newCaptures,
            ArrayBuilder<SyntaxNode?> newCapturesToClosureScopes,
            PooledDictionary<VariableCapture, int> capturesIndex,
            IMethodSymbol? liftingPrimaryConstructor,
            ArrayBuilder<int> reverseCapturesMap,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            bool isInsert,
            CancellationToken cancellationToken)
        {
            if (captures.Length == 0)
            {
                return;
            }

            var accessedCaptures = GetAccessedCaptures(lambdaBody, model, captures, capturesIndex, liftingPrimaryConstructor);

            var firstAccessedCaptureIndex = -1;
            for (var i = 0; i < captures.Length; i++)
            {
                var capture = captures[i];

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
                            if (capture.IsThis)
                            {
                                errorSpan = GetDiagnosticSpan(lambdaBody.GetLambda(), EditKind.Insert);
                            }
                            else
                            {
                                errorSpan = GetVariableUseSites(lambdaBody.GetExpressionsAndStatements(), capture.Symbol, model, cancellationToken).First().Span;
                            }

                            rudeEdit = RudeEditKind.InsertLambdaWithMultiScopeCapture;
                        }
                        else
                        {
                            errorSpan = GetSymbolLocationSpan(newCaptures[reverseCapturesMap.IndexOf(i)].Symbol, cancellationToken);
                            rudeEdit = RudeEditKind.DeleteLambdaWithMultiScopeCapture;
                        }

                        diagnostics.Add(new RudeEditDiagnostic(
                            rudeEdit,
                            errorSpan,
                            null,
                            new[] { GetDisplayName(lambdaBody.GetLambda()), captures[firstAccessedCaptureIndex].Name, capture.Name }));

                        break;
                    }
                }
            }
        }

        private static BitVector GetAccessedCaptures(
            LambdaBody lambdaBody,
            SemanticModel model,
            ImmutableArray<VariableCapture> captures,
            PooledDictionary<VariableCapture, int> capturesIndex,
            IMethodSymbol? liftingPrimaryConstructor)
        {
            var result = BitVector.Create(captures.Length);

            foreach (var expressionOrStatement in lambdaBody.GetExpressionsAndStatements())
            {
                var dataFlow = model.AnalyzeDataFlow(expressionOrStatement);
                MarkVariables(dataFlow.ReadInside);
                MarkVariables(dataFlow.WrittenInside);

                void MarkVariables(ImmutableArray<ISymbol> variables)
                {
                    foreach (var variable in variables)
                    {
                        if (capturesIndex.TryGetValue(VariableCapture.Create(variable, liftingPrimaryConstructor), out var newCaptureIndex))
                        {
                            result[newCaptureIndex] = true;
                        }
                    }
                }
            }

            return result;
        }

        private static void BuildIndex<TKey>(Dictionary<TKey, int> index, ImmutableArray<TKey> array)
            where TKey : notnull
        {
            for (var i = 0; i < array.Length; i++)
            {
                index.Add(array[i], i);
            }
        }

        internal static ISymbol? GetAssociatedMember(ISymbol symbol)
            => symbol switch
            {
                IMethodSymbol method => method.AssociatedSymbol,
                ITypeParameterSymbol or IParameterSymbol => symbol.ContainingSymbol,
                _ => null
            };

        /// <summary>
        /// Returns node that represents a declaration of the symbol.
        /// </summary>
        protected abstract SyntaxNode? GetSymbolDeclarationSyntax(ISymbol symbol, Func<ImmutableArray<SyntaxReference>, SyntaxReference?> selector, CancellationToken cancellationToken);

        protected SyntaxNode GetSymbolDeclarationSyntax(ISymbol symbol, CancellationToken cancellationToken)
            => GetSymbolDeclarationSyntax(symbol, selector: System.Linq.ImmutableArrayExtensions.First, cancellationToken)!;

        protected SyntaxNode? GetSingleSymbolDeclarationSyntax(ISymbol symbol, CancellationToken cancellationToken)
            => GetSymbolDeclarationSyntax(symbol, selector: refs => refs is [var single] ? single : null, cancellationToken)!;

        protected SyntaxNode? GetSymbolDeclarationSyntax(ISymbol symbol, SyntaxTree tree, CancellationToken cancellationToken)
            => GetSymbolDeclarationSyntax(symbol, syntaxRefs => syntaxRefs.FirstOrDefault(r => r.SyntaxTree == tree), cancellationToken);

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
            ImmutableArray<VariableCapture> oldCaptures,
            SyntaxNode? oldDeclaration,
            IMethodSymbol? oldPrimaryConstructor,
            ImmutableArray<VariableCapture> newCaptures,
            ISymbol newMember,
            SyntaxNode? newDeclaration,
            IMethodSymbol? newPrimaryConstructor,
            BidirectionalMap<SyntaxNode> bodyMap,
            [Out] ArrayBuilder<int> reverseCapturesMap,                  // {new capture index -> old capture index}
            [Out] ArrayBuilder<SyntaxNode?> newCapturesToClosureScopes,  // {new capture index -> new closure scope}
            [Out] ArrayBuilder<SyntaxNode?> oldCapturesToClosureScopes,  // {old capture index -> old closure scope}
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool hasErrors,
            CancellationToken cancellationToken)
        {
            hasErrors = false;

            BidirectionalMap<SyntaxNode>? parameterMap = null;
            if (oldDeclaration != null && newDeclaration != null)
            {
                parameterMap = ComputeParameterMap(oldDeclaration, newDeclaration);

                // In context where primary parameters are accessed directly but they are not part of the member body match (i.e. in initializers),
                // calculate mapping of the primary constructor parameters and merge it into parameter map.
                if (oldPrimaryConstructor != null &&
                    newPrimaryConstructor != null &&
                    IsDeclarationWithInitializer(oldDeclaration) &&
                    IsDeclarationWithInitializer(newDeclaration))
                {
                    var primaryParameterMap = ComputeParameterMap(
                        GetSymbolDeclarationSyntax(oldPrimaryConstructor, cancellationToken),
                        GetSymbolDeclarationSyntax(newPrimaryConstructor, cancellationToken));

                    Contract.ThrowIfNull(primaryParameterMap);

                    parameterMap = (parameterMap != null) ? parameterMap.Value.With(primaryParameterMap.Value) : primaryParameterMap;
                }
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

            ISymbol? oldCapturedThisParameter = null;
            ISymbol? newCapturedThisParameter = null;

            for (var oldCaptureIndex = 0; oldCaptureIndex < oldCaptures.Length; oldCaptureIndex++)
            {
                var oldCapture = oldCaptures[oldCaptureIndex];

                if (oldCapture.IsThis)
                {
                    oldCapturedThisParameter ??= oldCapture.Symbol;
                }
                else if (oldCapture.Symbol is IParameterSymbol oldParameterCapture)
                {
                    oldParameterCaptures.Add(GetParameterKey(oldParameterCapture, cancellationToken), oldCaptureIndex);
                }
                else
                {
                    oldLocalCaptures.Add(GetSymbolDeclarationSyntax(oldCapture.Symbol, cancellationToken), oldCaptureIndex);
                }
            }

            for (var newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
            {
                var newCapture = newCaptures[newCaptureIndex];
                int oldCaptureIndex;

                if (newCapture.IsThis)
                {
                    newCapturedThisParameter ??= newCapture.Symbol;
                    continue;
                }

                if (newCapture.Symbol is IParameterSymbol newParameterCapture)
                {
                    var newParameterKey = GetParameterKey(newParameterCapture, cancellationToken);
                    if (!TryMapParameter(newParameterKey, parameterMap?.Reverse, bodyMap.Reverse, out var oldParameterKey) ||
                        !oldParameterCaptures.TryGetValue(oldParameterKey, out oldCaptureIndex))
                    {
                        // parameter doesn't exist or is not captured prior to the edit:
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            GetSymbolLocationSpan(newParameterCapture, cancellationToken),
                            node: null,
                            new[] { newParameterCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old parameter capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldParameterCaptures.Remove(oldParameterKey);
                }
                else
                {
                    var local = newCapture.Symbol;
                    var newCaptureSyntax = GetSymbolDeclarationSyntax(local, cancellationToken);

                    // variable doesn't exists in the old method or has not been captured prior the edit:
                    if (!bodyMap.Reverse.TryGetValue(newCaptureSyntax, out var mappedOldSyntax) ||
                        !oldLocalCaptures.TryGetValue(mappedOldSyntax, out oldCaptureIndex))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            GetSymbolLocationSpan(local, cancellationToken),
                            node: null,
                            new[] { local.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldLocalCaptures.Remove(mappedOldSyntax);
                }

                reverseCapturesMap[newCaptureIndex] = oldCaptureIndex;

                var oldCapture = oldCaptures[oldCaptureIndex];
                Contract.ThrowIfTrue(oldCapture.IsThis);

                // If new parameter/local capture and does not have a corresponding old parameter/local capture a rude edit is reported above.
                // Also range variables can't be mapped to other variables since they have 
                // different kinds of declarator syntax nodes.
                Debug.Assert(oldCapture.Kind == newCapture.Kind);

                var oldSymbol = oldCapture.Symbol;
                var newSymbol = newCapture.Symbol;

                // Range variables don't have types. Each transparent identifier (range variable use)
                // might have a different type. Changing these types is ok as long as the containing lambda
                // signatures remain unchanged, which we validate for all lambdas in general.
                // 
                // The scope of a transparent identifier is the containing lambda body. Since we verify that
                // each lambda body accesses the same captured variables (including range variables) 
                // the corresponding scopes are guaranteed to be preserved as well.
                if (oldSymbol.Kind == SymbolKind.RangeVariable)
                {
                    continue;
                }

                // rename:
                // Note that the name has to match exactly even in VB, since we can't rename a field.
                // Consider: We could allow rename by emitting some special debug info for the field.
                if (newSymbol.Name != oldSymbol.Name)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.RenamingCapturedVariable,
                        GetSymbolLocationSpan(newSymbol, cancellationToken),
                        null,
                        new[] { oldSymbol.Name, newSymbol.Name }));

                    hasErrors = true;
                    continue;
                }

                // type check
                var oldType = GetType(oldSymbol);
                var newType = GetType(newSymbol);

                if (!TypesEquivalent(oldType, newType, exact: false))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingCapturedVariableType,
                        GetSymbolLocationSpan(newSymbol, cancellationToken),
                        node: null,
                        new[] { newSymbol.Name, oldType.ToDisplayString(ErrorDisplayFormat) }));

                    hasErrors = true;
                    continue;
                }

                // scope check for local variables (parameters can't change scope):
                if (oldSymbol.Kind != SymbolKind.Parameter)
                {
                    var oldScope = GetCapturedLocalScope(oldSymbol, cancellationToken);
                    var newScope = GetCapturedLocalScope(newSymbol, cancellationToken);
                    if (!AreEquivalentClosureScopes(oldScope, newScope, bodyMap.Reverse))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.ChangingCapturedVariableScope,
                            GetSymbolLocationSpan(newSymbol, cancellationToken),
                            node: null,
                            new[] { newSymbol.Name }));

                        hasErrors = true;
                        continue;
                    }

                    newCapturesToClosureScopes[newCaptureIndex] = newScope;
                    oldCapturesToClosureScopes[oldCaptureIndex] = oldScope;
                }
            }

            if (oldCapturedThisParameter is null != newCapturedThisParameter is null)
            {
                if (oldCapturedThisParameter == null)
                {
                    Debug.Assert(newCapturedThisParameter != null);

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
            DiagnosticContext diagnosticContext,
            SyntaxNode oldLambda,
            SyntaxNode newLambda,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasSignatureErrors,
            CancellationToken cancellationToken)
        {
            hasSignatureErrors = false;

            Debug.Assert(IsNestedFunction(newLambda));
            Debug.Assert(IsNestedFunction(oldLambda));

            if (IsLocalFunction(oldLambda) != IsLocalFunction(newLambda))
            {
                diagnosticContext.Report(RudeEditKind.SwitchBetweenLambdaAndLocalFunction, cancellationToken);
                hasSignatureErrors = true;
                return;
            }

            var oldLambdaSymbol = (IMethodSymbol)diagnosticContext.RequiredOldSymbol;
            var newLambdaSymbol = (IMethodSymbol)diagnosticContext.RequiredNewSymbol;

            // signature validation:
            if (!ParameterTypesEquivalent(oldLambdaSymbol.Parameters, newLambdaSymbol.Parameters, exact: false))
            {
                diagnosticContext.Report(RudeEditKind.ChangingLambdaParameters, cancellationToken);
                hasSignatureErrors = true;
            }
            else if (!ReturnTypesEquivalent(oldLambdaSymbol, newLambdaSymbol, exact: false))
            {
                diagnosticContext.Report(RudeEditKind.ChangingLambdaReturnType, cancellationToken);
                hasSignatureErrors = true;
            }
            else if (!TypeParametersEquivalent(oldLambdaSymbol.TypeParameters, newLambdaSymbol.TypeParameters, exact: false) ||
                     !oldLambdaSymbol.TypeParameters.SequenceEqual(newLambdaSymbol.TypeParameters, static (p, q) => p.Name == q.Name))
            {
                diagnosticContext.Report(RudeEditKind.ChangingTypeParameters, cancellationToken);
                hasSignatureErrors = true;
            }

            if (hasSignatureErrors)
            {
                return;
            }

            // custom attributes

            ReportCustomAttributeRudeEdits(diagnosticContext, capabilities, out _, out _, cancellationToken);

            for (var i = 0; i < oldLambdaSymbol.Parameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnosticContext.WithSymbols(oldLambdaSymbol.Parameters[i], newLambdaSymbol.Parameters[i]), capabilities, out _, out _, cancellationToken);
            }

            for (var i = 0; i < oldLambdaSymbol.TypeParameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnosticContext.WithSymbols(oldLambdaSymbol.TypeParameters[i], newLambdaSymbol.TypeParameters[i]), capabilities, out _, out _, cancellationToken);
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

        private static void ReportMissingStateMachineAttribute(
            in DiagnosticContext diagnosticContext,
            Compilation oldCompilation,
            StateMachineInfo kinds,
            CancellationToken cancellationToken)
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
                diagnosticContext.Report(RudeEditKind.UpdatingStateMachineMethodMissingAttribute, cancellationToken, arguments: new[] { stateMachineAttributeQualifiedName });
            }
        }

        #endregion

        #endregion

        #region Helpers

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
            public static readonly TypedConstantComparer Instance = new();

            public bool Equals(TypedConstant x, TypedConstant y)
                => x.Kind == y.Kind &&
                   x.IsNull == y.IsNull &&
                   SymbolEquivalenceComparer.Instance.Equals(x.Type, y.Type) &&
                   x.Kind switch
                   {
                       TypedConstantKind.Array => x.Values.IsDefault || x.Values.SequenceEqual(y.Values, Instance),
                       TypedConstantKind.Type => TypesEquivalent((ITypeSymbol?)x.Value, (ITypeSymbol?)y.Value, exact: false),
                       _ => Equals(x.Value, y.Value)
                   };

            public int GetHashCode(TypedConstant obj)
                => obj.GetHashCode();
        }

        private sealed class NamedArgumentComparer : IEqualityComparer<KeyValuePair<string, TypedConstant>>
        {
            public static readonly NamedArgumentComparer Instance = new();

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

        /// <summary>
        /// True if <paramref name="symbol"/> being inserted or deleted affects the bodies of synthesized record members.
        /// </summary>
        private static bool SymbolPresenceAffectsSynthesizedRecordMembers(ISymbol symbol)
            => symbol is { IsStatic: false, ContainingType.IsRecord: true } and
               (IPropertySymbol { GetMethod.IsImplicitlyDeclared: false, SetMethod: null or { IsImplicitlyDeclared: false } } or IFieldSymbol);

        /// <summary>
        /// True if a syntactic delete edit of an <paramref name="oldSymbol"/> in <paramref name="oldCompilation"/>
        /// that has a corresponding <paramref name="newSymbol"/> in the new compilation implies an existance
        /// of a matching syntactic insert edit (either in the currently analyzed document or another one).
        /// 
        /// The old symbol has to be explicitly declared, otherwise it couldn't have been deleted via syntactic delete edit.
        /// Only detects scenarios where an insert must have occurred. False doesn't mean an insert does not exist.
        /// </summary>
        private bool DeleteEditImpliesInsertEdit(ISymbol oldSymbol, ISymbol newSymbol, Compilation oldCompilation, CancellationToken cancellationToken)
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
                var oldProperty = (IPropertySymbol)oldSymbol;

                // An insert exists if the new primary parameter is explicitly declared and
                // the old one doesn't exist, is synthesized, or is not a primary constructor parameter.
                // A syntax change is causing the old synthesized property to be deleted,
                // so there has to be an insert inserting the new one.
                // 
                // old:
                // 
                // record R() { int P { get; init; } }              // insert exists: oldParameter == null
                // record R() { R(int P) {} int P { get; init; } }  // insert exists: old constructor is not primary
                // record R(int P) { int P { get; init; } }         // no insert
                // record R(int P);                                 // insert exists: oldProperty is synthesized auto-prop
                // 
                // new:
                //
                // record R(int P);

                return !newPrimaryParameter.IsSynthesized() &&
                    (oldParameter == null || oldParameter.IsSynthesized() || !IsPrimaryConstructor(oldParameter.ContainingSymbol, cancellationToken) || oldProperty.IsSynthesizedAutoProperty());
            }

            // Accessor of a property is synthesized based on presence of the property:
            if (newSymbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol { } newProperty })
            {
                var oldProperty = ((IMethodSymbol)oldSymbol).AssociatedSymbol;
                Contract.ThrowIfNull(oldProperty);

                // An insert exists if an insert exists for the new property
                return DeleteEditImpliesInsertEdit(oldProperty, newProperty, oldCompilation, cancellationToken);
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

            internal BidirectionalMap<SyntaxNode> IncludeLambdaBodyMaps(
                BidirectionalMap<SyntaxNode> bodyMatch,
                ArrayBuilder<ActiveNode> memberBodyActiveNodes,
                ref Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas)
            {
                return _abstractEditAndContinueAnalyzer.IncludeLambdaBodyMaps(bodyMatch, memberBodyActiveNodes, ref lazyActiveOrMatchedLambdas);
            }
        }

        #endregion
    }
}
