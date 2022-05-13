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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
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
        /// If the specified node represents a member declaration returns a node that represents its body,
        /// i.e. a node used as the root of statement-level match.
        /// </summary>
        /// <param name="node">A node representing a declaration or a top-level edit node.</param>
        /// 
        /// <returns>
        /// Returns null for nodes that don't represent declarations.
        /// </returns>
        /// <remarks>
        /// The implementation has to decide what kinds of nodes in top-level match relationship represent a declaration.
        /// Every member declaration must be represented by exactly one node, but not all nodes have to represent a declaration.
        /// 
        /// If a member doesn't have a body (null is returned) it can't have associated active statements.
        /// 
        /// Body does not need to cover all active statements that may be associated with the member. 
        /// E.g. Body of a C# constructor is the method body block. Active statements may be placed on the base constructor call.
        ///      Body of a VB field declaration with shared AsNew initializer is the New expression. Active statements might be placed on the field variables.
        /// <see cref="FindStatementAndPartner"/> has to account for such cases.
        /// </remarks>
        internal abstract SyntaxNode? TryGetDeclarationBody(SyntaxNode node);

        /// <summary>
        /// True if the specified <paramref name="declaration"/> node shares body with another declaration.
        /// </summary>
        internal abstract bool IsDeclarationWithSharedBody(SyntaxNode declaration);

        /// <summary>
        /// If the specified node represents a member declaration returns all tokens of the member declaration
        /// that might be covered by an active statement.
        /// </summary>
        /// <returns>
        /// Tokens covering all possible breakpoint spans associated with the member, 
        /// or null if the specified node doesn't represent a member declaration or 
        /// doesn't have a body that can contain active statements.
        /// </returns>
        /// <remarks>
        /// The implementation has to decide what kinds of nodes in top-level match relationship represent a declaration.
        /// Every member declaration must be represented by exactly one node, but not all nodes have to represent a declaration.
        /// 
        /// TODO: consider implementing this via <see cref="GetActiveSpanEnvelope"/>.
        /// </remarks>
        internal abstract IEnumerable<SyntaxToken>? TryGetActiveTokens(SyntaxNode node);

        /// <summary>
        /// Returns a span that contains all possible breakpoint spans of the <paramref name="declaration"/>
        /// and no breakpoint spans that do not belong to the <paramref name="declaration"/>.
        /// 
        /// Returns default if the declaration does not have any breakpoint spans.
        /// </summary>
        internal abstract (TextSpan envelope, TextSpan hole) GetActiveSpanEnvelope(SyntaxNode declaration);

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
        /// Finds a statement at given span and a declaration body.
        /// Also returns the corresponding partner statement in <paramref name="partnerDeclarationBody"/>, if specified.
        /// </summary>
        /// <remarks>
        /// The declaration body node may not contain the <paramref name="span"/>. 
        /// This happens when an active statement associated with the member is outside of its body
        /// (e.g. C# constructor, or VB <c>Dim a,b As New T</c>).
        /// If the position doesn't correspond to any statement uses the start of the <paramref name="declarationBody"/>.
        /// </remarks>
        protected abstract SyntaxNode FindStatementAndPartner(SyntaxNode declarationBody, TextSpan span, SyntaxNode? partnerDeclarationBody, out SyntaxNode? partner, out int statementPart);

        private SyntaxNode FindStatement(SyntaxNode declarationBody, TextSpan span, out int statementPart)
            => FindStatementAndPartner(declarationBody, span, null, out _, out statementPart);

        /// <summary>
        /// Maps <paramref name="leftNode"/> of a body of <paramref name="leftDeclaration"/> to corresponding body node
        /// of <paramref name="rightDeclaration"/>, assuming that the declaration bodies only differ in trivia.
        /// </summary>
        internal abstract SyntaxNode FindDeclarationBodyPartner(SyntaxNode leftDeclaration, SyntaxNode rightDeclaration, SyntaxNode leftNode);

        /// <summary>
        /// Returns a node that represents a body of a lambda containing specified <paramref name="node"/>,
        /// or null if the node isn't contained in a lambda. If a node is returned it must uniquely represent the lambda,
        /// i.e. be no two distinct nodes may represent the same lambda.
        /// </summary>
        protected abstract SyntaxNode? FindEnclosingLambdaBody(SyntaxNode? container, SyntaxNode node);

        /// <summary>
        /// Given a node that represents a lambda body returns all nodes of the body in a syntax list.
        /// </summary>
        /// <remarks>
        /// Note that VB lambda bodies are represented by a lambda header and that some lambda bodies share 
        /// their parent nodes with other bodies (e.g. join clause expressions).
        /// </remarks>
        protected abstract IEnumerable<SyntaxNode> GetLambdaBodyExpressionsAndStatements(SyntaxNode lambdaBody);

        protected abstract SyntaxNode? TryGetPartnerLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda);

        protected abstract Match<SyntaxNode> ComputeTopLevelMatch(SyntaxNode oldCompilationUnit, SyntaxNode newCompilationUnit);
        protected abstract Match<SyntaxNode> ComputeBodyMatch(SyntaxNode oldBody, SyntaxNode newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches);
        protected abstract Match<SyntaxNode> ComputeTopLevelDeclarationMatch(SyntaxNode oldDeclaration, SyntaxNode newDeclaration);
        protected abstract IEnumerable<SequenceEdit> GetSyntaxSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes);

        /// <summary>
        /// Matches old active statement to new active statement without constructing full method body match.
        /// This is needed for active statements that are outside of method body, like constructor initializer.
        /// </summary>
        protected abstract bool TryMatchActiveStatement(
            SyntaxNode oldStatement,
            int statementPart,
            SyntaxNode oldBody,
            SyntaxNode newBody,
            [NotNullWhen(true)] out SyntaxNode? newStatement);

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
        /// True if both nodes represent the same kind of suspension point 
        /// (await expression, await foreach statement, await using declarator, yield return, yield break).
        /// </summary>
        protected virtual bool StateMachineSuspensionPointKindEquals(SyntaxNode suspensionPoint1, SyntaxNode suspensionPoint2)
            => suspensionPoint1.RawKind == suspensionPoint2.RawKind;

        /// <summary>
        /// Determines if two syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        protected abstract bool AreEquivalent(SyntaxNode left, SyntaxNode right);

        /// <summary>
        /// Returns true if the code emitted for the old active statement part (<paramref name="statementPart"/> of <paramref name="oldStatement"/>) 
        /// is the same as the code emitted for the corresponding new active statement part (<paramref name="statementPart"/> of <paramref name="newStatement"/>). 
        /// </summary>
        /// <remarks>
        /// A rude edit is reported if an active statement is changed and this method returns true.
        /// </remarks>
        protected abstract bool AreEquivalentActiveStatements(SyntaxNode oldStatement, SyntaxNode newStatement, int statementPart);

        protected abstract bool IsCompilationUnitWithGlobalStatements(SyntaxNode node);
        protected abstract bool IsGlobalStatement(SyntaxNode node);
        protected abstract TextSpan GetGlobalStatementDiagnosticSpan(SyntaxNode node);

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
        /// Analyzes data flow in the member body represented by the specified node and returns all captured variables and parameters (including "this").
        /// If the body is a field/property initializer analyzes the initializer expression only.
        /// </summary>
        protected abstract ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model, SyntaxNode memberBody);

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
        protected abstract void GetStateMachineInfo(SyntaxNode body, out ImmutableArray<SyntaxNode> suspensionPoints, out StateMachineKinds kinds);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, TextSpan newStatementSpan);
        internal abstract void ReportOtherRudeEditsAroundActiveStatement(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode oldStatement, SyntaxNode newStatement, bool isNonLeaf);
        internal abstract void ReportMemberBodyUpdateRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode newMember, TextSpan? span);
        internal abstract void ReportInsertedMemberSymbolRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol newSymbol, SyntaxNode newNode, bool insertingIntoExistingContainingType);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract bool IsLambda(SyntaxNode node);
        internal abstract bool IsInterfaceDeclaration(SyntaxNode node);
        internal abstract bool IsRecordDeclaration(SyntaxNode node);

        /// <summary>
        /// True if the node represents any form of a function definition nested in another function body (i.e. anonymous function, lambda, local function).
        /// </summary>
        internal abstract bool IsNestedFunction(SyntaxNode node);

        internal abstract bool IsLocalFunction(SyntaxNode node);
        internal abstract bool IsClosureScope(SyntaxNode node);
        internal abstract bool ContainsLambda(SyntaxNode declaration);
        internal abstract SyntaxNode GetLambda(SyntaxNode lambdaBody);
        internal abstract IMethodSymbol GetLambdaExpressionSymbol(SemanticModel model, SyntaxNode lambdaExpression, CancellationToken cancellationToken);
        internal abstract SyntaxNode? GetContainingQueryExpression(SyntaxNode node);
        internal abstract bool QueryClauseLambdasTypeEquivalent(SemanticModel oldModel, SyntaxNode oldNode, SemanticModel newModel, SyntaxNode newNode, CancellationToken cancellationToken);

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
        internal abstract bool TryGetLambdaBodies(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? body1, out SyntaxNode? body2);

        internal abstract bool IsStateMachineMethod(SyntaxNode declaration);

        /// <summary>
        /// Returns the type declaration that contains a specified <paramref name="node"/>.
        /// This can be class, struct, interface, record or enum declaration.
        /// </summary>
        internal abstract SyntaxNode? TryGetContainingTypeDeclaration(SyntaxNode node);

        /// <summary>
        /// Returns the declaration of 
        /// - a property, indexer or event declaration whose accessor is the specified <paramref name="node"/>,
        /// - a method, an indexer or a type (delegate) if the <paramref name="node"/> is a parameter,
        /// - a method or an type if the <paramref name="node"/> is a type parameter.
        /// </summary>
        internal abstract bool TryGetAssociatedMemberDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? declaration);

        internal abstract bool HasBackingField(SyntaxNode propertyDeclaration);

        /// <summary>
        /// Return true if the declaration is a field/property declaration with an initializer. 
        /// Shall return false for enum members.
        /// </summary>
        internal abstract bool IsDeclarationWithInitializer(SyntaxNode declaration);

        /// <summary>
        /// Return true if the declaration is a parameter that is part of a records primary constructor.
        /// </summary>
        internal abstract bool IsRecordPrimaryConstructorParameter(SyntaxNode declaration);

        /// <summary>
        /// Return true if the declaration is a property accessor for a property that represents one of the parameters in a records primary constructor.
        /// </summary>
        internal abstract bool IsPropertyAccessorDeclarationMatchingPrimaryConstructorParameter(SyntaxNode declaration, INamedTypeSymbol newContainingType, out bool isFirstAccessor);

        /// <summary>
        /// Return true if the declaration is a constructor declaration to which field/property initializers are emitted. 
        /// </summary>
        internal abstract bool IsConstructorWithMemberInitializers(SyntaxNode declaration);

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
            DocumentAnalysisResults.Log.Write("Analyzing document {0}", newDocument.Name);

            Debug.Assert(!newActiveStatementSpans.IsDefault);
            Debug.Assert(newDocument.SupportsSyntaxTree);
            Debug.Assert(newDocument.SupportsSemanticModel);

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
                    oldTree = await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(oldTree);

                    oldRoot = await oldTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
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
                var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
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
                    DocumentAnalysisResults.Log.Write("{0}: syntax errors", newDocument.Name);
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, ImmutableArray<RudeEditDiagnostic>.Empty, syntaxError, hasChanges);
                }

                if (!hasChanges)
                {
                    // The document might have been closed and reopened, which might have triggered analysis. 
                    // If the document is unchanged don't continue the analysis since 
                    // a) comparing texts is cheaper than diffing trees
                    // b) we need to ignore errors in unchanged documents

                    DocumentAnalysisResults.Log.Write("{0}: unchanged", newDocument.Name);
                    return DocumentAnalysisResults.Unchanged(newDocument.Id);
                }

                // Disallow modification of a file with experimental features enabled.
                // These features may not be handled well by the analysis below.
                if (ExperimentalFeaturesEnabled(newTree))
                {
                    DocumentAnalysisResults.Log.Write("{0}: experimental features enabled", newDocument.Name);

                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.ExperimentalFeaturesEnabled, default)), syntaxError: null, hasChanges);
                }

                var capabilities = new EditAndContinueCapabilitiesGrantor(await lazyCapabilities.GetValueAsync(cancellationToken).ConfigureAwait(false));
                var oldActiveStatementMap = await lazyOldActiveStatementMap.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // If the document has changed at all, lets make sure Edit and Continue is supported
                if (!capabilities.Grant(EditAndContinueCapabilities.Baseline))
                {
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, ImmutableArray.Create(
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
                var hasRudeEdits = false;

                ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits, editMap);

                if (diagnostics.Count > 0 && !hasRudeEdits)
                {
                    DocumentAnalysisResults.Log.Write("{0} syntactic rude edits, first: '{1}'", diagnostics.Count, newDocument.FilePath);
                    hasRudeEdits = true;
                }

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

                if (diagnostics.Count > 0 && !hasRudeEdits)
                {
                    DocumentAnalysisResults.Log.Write("{0}@{1}: rude edit ({2} total)", newDocument.FilePath, diagnostics.First().Span.Start, diagnostics.Count);
                    hasRudeEdits = true;
                }

                return new DocumentAnalysisResults(
                    newDocument.Id,
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
                var diagnostic = (e is OutOfMemoryException) ?
                    new RudeEditDiagnostic(RudeEditKind.SourceFileTooBig, span: default, arguments: new[] { newDocument.FilePath }) :
                    new RudeEditDiagnostic(RudeEditKind.InternalError, span: default, arguments: new[] { newDocument.FilePath, e.ToString() });

                // Report as "syntax error" - we can't analyze the document
                return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, ImmutableArray.Create(diagnostic), syntaxError: null, hasChanges);
            }
        }

        private void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, EditScript<SyntaxNode> syntacticEdits, Dictionary<SyntaxNode, EditKind> editMap)
        {
            foreach (var edit in syntacticEdits.Edits)
            {
                ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits.Match, edit, editMap);
            }
        }

        /// <summary>
        /// Reports rude edits for a symbol that's been deleted in one location and inserted in another and the edit was not classified as
        /// <see cref="EditKind.Move"/> or <see cref="EditKind.Reorder"/>.
        /// The scenarios include moving a type declaration from one file to another and moving a member of a partial type from one partial declaration to another.
        /// </summary>
        internal virtual void ReportDeclarationInsertDeleteRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode, ISymbol oldSymbol, ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities, CancellationToken cancellationToken)
        {
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
                    ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingParameterTypes, newSymbol, newNode, cancellationToken);
                }
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
                                DocumentAnalysisResults.Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
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
                                var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody, oldMember.FindToken(adjustedOldStatementStart).Parent!);
                                var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody, trackedStatement);
                                if (oldEnclosingLambdaBody == newEnclosingLambdaBody)
                                {
                                    newStatement = trackedStatement;
                                    statementPart = trackedStatementPart;
                                }
                            }

                            if (newStatement == null)
                            {
                                Contract.ThrowIfFalse(statementPart == -1);
                                FindStatementAndPartner(oldBody, oldStatementSpan, newBody, out newStatement, out statementPart);
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
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
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

        internal readonly struct ActiveNode
        {
            public readonly int ActiveStatementIndex;
            public readonly SyntaxNode OldNode;
            public readonly SyntaxNode? NewTrackedNode;
            public readonly SyntaxNode? EnclosingLambdaBody;
            public readonly int StatementPart;

            public ActiveNode(int activeStatementIndex, SyntaxNode oldNode, SyntaxNode? enclosingLambdaBody, int statementPart, SyntaxNode? newTrackedNode)
            {
                ActiveStatementIndex = activeStatementIndex;
                OldNode = oldNode;
                NewTrackedNode = newTrackedNode;
                EnclosingLambdaBody = enclosingLambdaBody;
                StatementPart = statementPart;
            }
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
            public readonly SyntaxNode? NewBody;

            public LambdaInfo(List<int> activeNodeIndices)
                : this(activeNodeIndices, null, null)
            {
            }

            private LambdaInfo(List<int>? activeNodeIndices, Match<SyntaxNode>? match, SyntaxNode? newLambdaBody)
            {
                ActiveNodeIndices = activeNodeIndices;
                Match = match;
                NewBody = newLambdaBody;
            }

            public LambdaInfo WithMatch(Match<SyntaxNode> match, SyntaxNode newLambdaBody)
                => new(ActiveNodeIndices, match, newLambdaBody);
        }

        private void AnalyzeChangedMemberBody(
            SyntaxNode oldDeclaration,
            SyntaxNode newDeclaration,
            SyntaxNode oldBody,
            SyntaxNode? newBody,
            SemanticModel oldModel,
            SemanticModel newModel,
            ISymbol oldSymbol,
            ISymbol newSymbol,
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

            var activeStatementIndices = GetOverlappingActiveStatements(oldDeclaration, oldActiveStatements);

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
                ReportMemberBodyUpdateRudeEdits(diagnostics, newDeclaration, GetDiagnosticSpan(newDeclaration, EditKind.Update));

                _testFaultInjector?.Invoke(newBody);

                // Populated with active lambdas and matched lambdas. 
                // Unmatched non-active lambdas are not included.
                // { old-lambda-body -> info }
                Dictionary<SyntaxNode, LambdaInfo>? lazyActiveOrMatchedLambdas = null;

                // finds leaf nodes that correspond to the old active statements:
                using var _ = ArrayBuilder<ActiveNode>.GetInstance(out var activeNodes);
                foreach (var activeStatementIndex in activeStatementIndices)
                {
                    var oldStatementSpan = oldActiveStatements[activeStatementIndex].UnmappedSpan;

                    var oldStatementSyntax = FindStatement(oldBody, oldStatementSpan, out var statementPart);
                    Contract.ThrowIfNull(oldStatementSyntax);

                    var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody, oldStatementSyntax);
                    if (oldEnclosingLambdaBody != null)
                    {
                        lazyActiveOrMatchedLambdas ??= new Dictionary<SyntaxNode, LambdaInfo>();

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
                        var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody, newStatementSyntax);

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

                var bodyMatch = ComputeBodyMatch(oldBody, newBody, activeNodes.Where(n => n.EnclosingLambdaBody == null).ToArray(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
                var map = ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics);

                if (oldHasStateMachineSuspensionPoint)
                {
                    ReportStateMachineRudeEdits(oldModel.Compilation, oldSymbol, newBody, diagnostics);
                }
                else if (newHasStateMachineSuspensionPoint &&
                    !capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                {
                    // Adding a state machine, either for async or iterator, will require creating a new helper class
                    // so is a rude edit if the runtime doesn't support it
                    if (newSymbol is IMethodSymbol { IsAsync: true })
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.MakeMethodAsyncNotSupportedByRuntime, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                    }
                    else
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.MakeMethodIteratorNotSupportedByRuntime, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                    }
                }

                ReportLambdaAndClosureRudeEdits(
                    oldModel,
                    oldBody,
                    newModel,
                    newBody,
                    newSymbol,
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
                    newHasStateMachineSuspensionPoint ||
                    newBodyHasLambdas ||
                    IsConstructorWithMemberInitializers(newDeclaration) ||
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

                        hasMatching = TryMatchActiveStatement(oldStatementSyntax, statementPart, oldBody, newBody, out newStatementSyntax) ||
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

                            hasMatching = TryMatchActiveStatement(oldStatementSyntax, statementPart, oldEnclosingLambdaBody, newEnclosingLambdaBody, out newStatementSyntax) ||
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
                        var oldLambda = GetLambda(oldEnclosingLambdaBody);
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

                    // exception handling around the statement:
                    CalculateExceptionRegionsAroundActiveStatement(
                        bodyMatch,
                        oldStatementSyntax,
                        newStatementSyntax,
                        newSpan,
                        activeStatementIndex,
                        isNonLeaf,
                        newExceptionRegions,
                        diagnostics,
                        cancellationToken);

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

                // We expect OOM to be thrown during the analysis if the number of statements is too large.
                // In such case we report a rude edit for the document. If the host is actually running out of memory,
                // it might throw another OOM here or later on.
                if (e is OutOfMemoryException)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.MemberBodyTooBig,
                        GetBodyDiagnosticSpan(newBody, EditKind.Update),
                        newBody,
                        arguments: new[] { GetBodyDisplayName(newBody) }));
                }
                else
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.MemberBodyInternalError,
                        GetBodyDiagnosticSpan(newBody, EditKind.Update),
                        newBody,
                        arguments: new[] { GetBodyDisplayName(newBody), e.ToString() }));
                }
            }
        }

        private bool TryGetTrackedStatement(ImmutableArray<LinePositionSpan> activeStatementSpans, int index, SourceText text, SyntaxNode declaration, SyntaxNode body, [NotNullWhen(true)] out SyntaxNode? trackedStatement, out int trackedStatementPart)
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

            trackedStatement = FindStatement(body, trackedSpan, out trackedStatementPart);
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
        /// Calculates a syntax map of the entire method body including all lambda bodies it contains (recursively).
        /// </summary>
        private BidirectionalMap<SyntaxNode> ComputeMap(
            Match<SyntaxNode> bodyMatch,
            ArrayBuilder<ActiveNode> activeNodes,
            ref Dictionary<SyntaxNode, LambdaInfo>? lazyActiveOrMatchedLambdas,
            ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            ArrayBuilder<Match<SyntaxNode>>? lambdaBodyMatches = null;
            var currentLambdaBodyMatch = -1;
            var currentBodyMatch = bodyMatch;

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
                        lazyActiveOrMatchedLambdas ??= new Dictionary<SyntaxNode, LambdaInfo>();

                        var newLambdaBody1 = TryGetPartnerLambdaBody(oldLambdaBody1, newNode);
                        if (newLambdaBody1 != null)
                        {
                            lambdaBodyMatches.Add(ComputeLambdaBodyMatch(oldLambdaBody1, newLambdaBody1, activeNodes, lazyActiveOrMatchedLambdas, diagnostics));
                        }

                        if (oldLambdaBody2 != null)
                        {
                            var newLambdaBody2 = TryGetPartnerLambdaBody(oldLambdaBody2, newNode);
                            if (newLambdaBody2 != null)
                            {
                                lambdaBodyMatches.Add(ComputeLambdaBodyMatch(oldLambdaBody2, newLambdaBody2, activeNodes, lazyActiveOrMatchedLambdas, diagnostics));
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
                return BidirectionalMap<SyntaxNode>.FromMatch(bodyMatch);
            }

            var map = new Dictionary<SyntaxNode, SyntaxNode>();
            var reverseMap = new Dictionary<SyntaxNode, SyntaxNode>();

            // include all matches, including the root:
            map.AddRange(bodyMatch.Matches);
            reverseMap.AddRange(bodyMatch.ReverseMatches);

            foreach (var lambdaBodyMatch in lambdaBodyMatches)
            {
                foreach (var pair in lambdaBodyMatch.Matches)
                {
                    if (!map.ContainsKey(pair.Key))
                    {
                        map[pair.Key] = pair.Value;
                        reverseMap[pair.Value] = pair.Key;
                    }
                }
            }

            lambdaBodyMatches?.Free();

            return new BidirectionalMap<SyntaxNode>(map, reverseMap);
        }

        private Match<SyntaxNode> ComputeLambdaBodyMatch(
            SyntaxNode oldLambdaBody,
            SyntaxNode newLambdaBody,
            IReadOnlyList<ActiveNode> activeNodes,
            [Out] Dictionary<SyntaxNode, LambdaInfo> activeOrMatchedLambdas,
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            ActiveNode[]? activeNodesInLambda;
            if (activeOrMatchedLambdas.TryGetValue(oldLambdaBody, out var info))
            {
                // Lambda may be matched but not be active.
                activeNodesInLambda = info.ActiveNodeIndices?.Select(i => activeNodes[i]).ToArray();
            }
            else
            {
                // If the lambda body isn't in the map it doesn't have any active/tracked statements.
                activeNodesInLambda = null;
                info = new LambdaInfo();
            }

            var lambdaBodyMatch = ComputeBodyMatch(oldLambdaBody,
                newLambdaBody, activeNodesInLambda ?? Array.Empty<ActiveNode>(),
                diagnostics, out _, out _);

            activeOrMatchedLambdas[oldLambdaBody] = info.WithMatch(lambdaBodyMatch, newLambdaBody);

            return lambdaBodyMatch;
        }

        private Match<SyntaxNode> ComputeBodyMatch(
            SyntaxNode oldBody,
            SyntaxNode newBody,
            ActiveNode[] activeNodes,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool oldHasStateMachineSuspensionPoint,
            out bool newHasStateMachineSuspensionPoint)
        {
            List<KeyValuePair<SyntaxNode, SyntaxNode>>? lazyKnownMatches = null;
            List<SequenceEdit>? lazyRudeEdits = null;
            GetStateMachineInfo(oldBody, out var oldStateMachineSuspensionPoints, out var oldStateMachineKinds);
            GetStateMachineInfo(newBody, out var newStateMachineSuspensionPoints, out var newStateMachineKinds);

            AddMatchingActiveNodes(ref lazyKnownMatches, activeNodes);

            // Consider following cases:
            // 1) Both old and new methods contain yields/awaits.
            //    Map the old suspension points to new ones, report errors for added/deleted suspension points.
            // 2) The old method contains yields/awaits but the new doesn't.
            //    Report rude edits for each deleted yield/await.
            // 3) The new method contains yields/awaits but the old doesn't.
            //    a) If the method has active statements report rude edits for each inserted yield/await (insert "around" an active statement).
            //    b) If the method has no active statements then the edit is valid, we don't need to calculate map.
            // 4) The old method is async/iterator, the new method is not and it contains an active statement.
            //    Report rude edit since we can't remap IP from MoveNext to the kickoff method.
            //    Note that iterators in VB don't need to contain yield, so this case is not covered by change in number of yields.

            var creatingStateMachineAroundActiveStatement = oldStateMachineSuspensionPoints.Length == 0 && newStateMachineSuspensionPoints.Length > 0 && activeNodes.Length > 0;
            oldHasStateMachineSuspensionPoint = oldStateMachineSuspensionPoints.Length > 0;
            newHasStateMachineSuspensionPoint = newStateMachineSuspensionPoints.Length > 0;

            if (oldStateMachineSuspensionPoints.Length > 0 || creatingStateMachineAroundActiveStatement)
            {
                AddMatchingStateMachineSuspensionPoints(ref lazyKnownMatches, ref lazyRudeEdits, oldStateMachineSuspensionPoints, newStateMachineSuspensionPoints);
            }

            var match = ComputeBodyMatch(oldBody, newBody, lazyKnownMatches);

            if (IsLocalFunction(match.OldRoot) && IsLocalFunction(match.NewRoot))
            {
                ReportMemberBodyUpdateRudeEdits(diagnostics, match.NewRoot, match.NewRoot.Span);
            }

            if (lazyRudeEdits != null)
            {
                foreach (var rudeEdit in lazyRudeEdits)
                {
                    if (rudeEdit.Kind == EditKind.Delete)
                    {
                        var deletedNode = oldStateMachineSuspensionPoints[rudeEdit.OldIndex];
                        ReportStateMachineSuspensionPointDeletedRudeEdit(diagnostics, match, deletedNode);
                    }
                    else
                    {
                        Debug.Assert(rudeEdit.Kind == EditKind.Insert);

                        var insertedNode = newStateMachineSuspensionPoints[rudeEdit.NewIndex];
                        ReportStateMachineSuspensionPointInsertedRudeEdit(diagnostics, match, insertedNode, creatingStateMachineAroundActiveStatement);
                    }
                }
            }
            else if (oldStateMachineSuspensionPoints.Length > 0)
            {
                Debug.Assert(oldStateMachineSuspensionPoints.Length == newStateMachineSuspensionPoints.Length);

                for (var i = 0; i < oldStateMachineSuspensionPoints.Length; i++)
                {
                    var oldNode = oldStateMachineSuspensionPoints[i];
                    var newNode = newStateMachineSuspensionPoints[i];

                    // changing yield return to yield break, await to await foreach, yield to await, etc.
                    if (StateMachineSuspensionPointKindEquals(oldNode, newNode))
                    {
                        Debug.Assert(StatementLabelEquals(oldNode, newNode));
                    }
                    else
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.ChangingStateMachineShape,
                            newNode.Span,
                            newNode,
                            new[] { GetSuspensionPointDisplayName(oldNode, EditKind.Update), GetSuspensionPointDisplayName(newNode, EditKind.Update) }));
                    }

                    ReportStateMachineSuspensionPointRudeEdits(diagnostics, oldNode, newNode);
                }
            }
            else if (activeNodes.Length > 0)
            {
                // It is allowed to update a regular method to an async method or an iterator.
                // The only restriction is a presence of an active statement in the method body
                // since the debugger does not support remapping active statements to a different method.
                if (oldStateMachineKinds != newStateMachineKinds)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement,
                        GetBodyDiagnosticSpan(newBody, EditKind.Update)));
                }
            }

            // report removing async as rude:
            if (lazyRudeEdits == null)
            {
                if ((oldStateMachineKinds & StateMachineKinds.Async) != 0 && (newStateMachineKinds & StateMachineKinds.Async) == 0)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingFromAsynchronousToSynchronous,
                        GetBodyDiagnosticSpan(newBody, EditKind.Update),
                        newBody,
                        new[] { GetBodyDisplayName(newBody) }));
                }

                // VB supports iterator lambdas/methods without yields
                if ((oldStateMachineKinds & StateMachineKinds.Iterator) != 0 && (newStateMachineKinds & StateMachineKinds.Iterator) == 0)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ModifiersUpdate,
                        GetBodyDiagnosticSpan(newBody, EditKind.Update),
                        newBody,
                        new[] { GetBodyDisplayName(newBody) }));
                }
            }

            return match;
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

        private static void AddMatchingActiveNodes(ref List<KeyValuePair<SyntaxNode, SyntaxNode>>? lazyKnownMatches, IEnumerable<ActiveNode> activeNodes)
        {
            // add nodes that are tracked by the editor buffer to known matches:
            foreach (var activeNode in activeNodes)
            {
                if (activeNode.NewTrackedNode != null)
                {
                    lazyKnownMatches ??= new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
                    lazyKnownMatches.Add(KeyValuePairUtil.Create(activeNode.OldNode, activeNode.NewTrackedNode));
                }
            }
        }

        private void AddMatchingStateMachineSuspensionPoints(
            ref List<KeyValuePair<SyntaxNode, SyntaxNode>>? lazyKnownMatches,
            ref List<SequenceEdit>? lazyRudeEdits,
            ImmutableArray<SyntaxNode> oldStateMachineSuspensionPoints,
            ImmutableArray<SyntaxNode> newStateMachineSuspensionPoints)
        {
            // State machine suspension points (yield statements, await expressions, await foreach loops, await using declarations) 
            // determine the structure of the generated state machine.
            // Change of the SM structure is far more significant then changes of the value (arguments) of these nodes.
            // Hence we build the match such that these nodes are fixed.

            lazyKnownMatches ??= new List<KeyValuePair<SyntaxNode, SyntaxNode>>();

            void AddMatch(ref List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches, int oldIndex, int newIndex)
            {
                var oldNode = oldStateMachineSuspensionPoints[oldIndex];
                var newNode = newStateMachineSuspensionPoints[newIndex];

                if (StatementLabelEquals(oldNode, newNode))
                {
                    lazyKnownMatches.Add(KeyValuePairUtil.Create(oldNode, newNode));
                }
            }

            if (oldStateMachineSuspensionPoints.Length == newStateMachineSuspensionPoints.Length)
            {
                for (var i = 0; i < oldStateMachineSuspensionPoints.Length; i++)
                {
                    AddMatch(ref lazyKnownMatches, i, i);
                }
            }
            else
            {
                // use LCS to provide better errors (deletes, inserts and updates)
                var edits = GetSyntaxSequenceEdits(oldStateMachineSuspensionPoints, newStateMachineSuspensionPoints);

                foreach (var edit in edits)
                {
                    var editKind = edit.Kind;

                    if (editKind == EditKind.Update)
                    {
                        AddMatch(ref lazyKnownMatches, edit.OldIndex, edit.NewIndex);
                    }
                    else
                    {
                        lazyRudeEdits ??= new List<SequenceEdit>();
                        lazyRudeEdits.Add(edit);
                    }
                }

                Debug.Assert(lazyRudeEdits != null);
            }
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

        private TextSpan GetDeletedNodeDiagnosticSpan(SyntaxNode deletedLambdaBody, Match<SyntaxNode> match, Dictionary<SyntaxNode, LambdaInfo> lambdaInfos)
        {
            var oldLambdaBody = deletedLambdaBody;
            while (true)
            {
                var oldParentLambdaBody = FindEnclosingLambdaBody(match.OldRoot, GetLambda(oldLambdaBody));
                if (oldParentLambdaBody == null)
                {
                    return GetDeletedNodeDiagnosticSpan(match.Matches, oldLambdaBody);
                }

                if (lambdaInfos.TryGetValue(oldParentLambdaBody, out var lambdaInfo) && lambdaInfo.Match != null)
                {
                    return GetDeletedNodeDiagnosticSpan(lambdaInfo.Match.Matches, oldLambdaBody);
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

        internal TextSpan GetDeletedNodeDiagnosticSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedNode)
        {
            var hasAncestor = TryGetMatchingAncestor(forwardMap, deletedNode, out var newAncestor);
            RoslynDebug.Assert(hasAncestor && newAncestor != null);
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

        private IEnumerable<int> GetOverlappingActiveStatements(SyntaxNode declaration, ImmutableArray<UnmappedActiveStatement> statements)
        {
            var (envelope, hole) = GetActiveSpanEnvelope(declaration);
            if (envelope == default)
            {
                yield break;
            }

            var range = ActiveStatementsMap.GetSpansStartingInSpan(
                envelope.Start,
                envelope.End,
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

                var newTokens = TryGetActiveTokens(newNode);
                if (newTokens == null)
                {
                    continue;
                }

                // A (rude) edit could have been made that changes whether the node may contain active statements,
                // so although the nodes match they might not have the same active tokens.
                // E.g. field declaration changed to const field declaration.
                var oldTokens = TryGetActiveTokens(oldNode);
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

                    // no update edit => tokens must match:
                    Debug.Assert(oldHasToken == newHasToken);

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
            => oldParameters.SequenceEqual(newParameters, exact, (oldParameter, newParameter, exact) => ParameterTypesEquivalent(oldParameter, newParameter, exact));

        protected static bool CustomModifiersEquivalent(CustomModifier oldModifier, CustomModifier newModifier, bool exact)
            => oldModifier.IsOptional == newModifier.IsOptional &&
               TypesEquivalent(oldModifier.Modifier, newModifier.Modifier, exact);

        protected static bool CustomModifiersEquivalent(ImmutableArray<CustomModifier> oldModifiers, ImmutableArray<CustomModifier> newModifiers, bool exact)
            => oldModifiers.SequenceEqual(newModifiers, exact, (x, y, exact) => CustomModifiersEquivalent(x, y, exact));

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

        private readonly struct ConstructorEdit
        {
            public readonly INamedTypeSymbol OldType;

            /// <summary>
            /// Contains syntax maps for all changed data member initializers or constructor declarations (of constructors emitting initializers)
            /// in the currently analyzed document. The key is the declaration of the member.
            /// </summary>
            public readonly Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode?>?> ChangedDeclarations;

            public ConstructorEdit(INamedTypeSymbol oldType)
            {
                OldType = oldType;
                ChangedDeclarations = new Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode?>?>();
            }
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
            PooledDictionary<INamedTypeSymbol, ConstructorEdit>? instanceConstructorEdits = null;
            PooledDictionary<INamedTypeSymbol, ConstructorEdit>? staticConstructorEdits = null;

            var oldModel = (oldDocument != null) ? await oldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false) : null;
            var newModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var oldCompilation = oldModel?.Compilation ?? await oldProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var newCompilation = newModel.Compilation;

            using var _1 = PooledHashSet<ISymbol>.GetInstance(out var processedSymbols);
            using var _2 = ArrayBuilder<SemanticEditInfo>.GetInstance(out var semanticEdits);

            try
            {
                INamedTypeSymbol? lazyLayoutAttribute = null;

                foreach (var edit in editScript.Edits)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (edit.Kind == EditKind.Move)
                    {
                        // Move is either a Rude Edit and already reported in syntax analysis, or has no semantic effect.
                        // For example, in VB we allow move from field multi-declaration.
                        // "Dim a, b As Integer" -> "Dim a As Integer" (update) and "Dim b As Integer" (move)
                        continue;
                    }

                    if (edit.Kind == EditKind.Reorder)
                    {
                        // Currently we don't do any semantic checks for reordering
                        // and we don't need to report them to the compiler either.
                        // Consider: Currently symbol ordering changes are not reflected in metadata (Reflection will report original order).

                        // Consider: Reordering of fields is not allowed since it changes the layout of the type.
                        // This ordering should however not matter unless the type has explicit layout so we might want to allow it.
                        // We do not check changes to the order if they occur across multiple documents (the containing type is partial).
                        Debug.Assert(!IsDeclarationWithInitializer(edit.OldNode) && !IsDeclarationWithInitializer(edit.NewNode));
                        continue;
                    }

                    foreach (var symbolEdits in GetSymbolEdits(edit.Kind, edit.OldNode, edit.NewNode, oldModel, newModel, editMap, cancellationToken))
                    {
                        Func<SyntaxNode, SyntaxNode?>? syntaxMap;
                        SemanticEditKind editKind;

                        var (oldSymbol, newSymbol, syntacticEditKind) = symbolEdits;
                        var symbol = newSymbol ?? oldSymbol;
                        Contract.ThrowIfNull(symbol);

                        if (!processedSymbols.Add(symbol))
                        {
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
                            if (oldSymbol == null || oldDeclaration != null && oldDeclaration.SyntaxTree != oldModel?.SyntaxTree)
                            {
                                syntacticEditKind = EditKind.Insert;
                            }
                            else if (newSymbol == null || newDeclaration != null && newDeclaration.SyntaxTree != newModel.SyntaxTree)
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

                                    var activeStatementIndices = GetOverlappingActiveStatements(oldDeclaration, oldActiveStatements);
                                    var hasActiveStatement = activeStatementIndices.Any();

                                    // TODO: if the member isn't a field/property we should return empty span.
                                    // We need to adjust the tracking span design and UpdateUneditedSpans to account for such empty spans.
                                    if (hasActiveStatement)
                                    {
                                        var newSpan = IsDeclarationWithInitializer(oldDeclaration) ?
                                            GetDeletedNodeActiveSpan(editScript.Match.Matches, oldDeclaration) :
                                            GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, oldDeclaration);

                                        foreach (var index in activeStatementIndices)
                                        {
                                            Debug.Assert(newActiveStatements[index] is null);

                                            newActiveStatements[index] = GetActiveStatementWithSpan(oldActiveStatements[index], editScript.Match.NewRoot.SyntaxTree, newSpan, diagnostics, cancellationToken);
                                            newExceptionRegions[index] = ImmutableArray<SourceFileSpan>.Empty;
                                        }
                                    }

                                    syntaxMap = null;
                                    editKind = SemanticEditKind.Delete;

                                    // Check if the declaration has been moved from one document to another.
                                    if (newSymbol != null && !(newSymbol is IMethodSymbol newMethod && newMethod.IsPartialDefinition))
                                    {
                                        // Symbol has actually not been deleted but rather moved to another document, another partial type declaration
                                        // or replaced with an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)

                                        // Report rude edit if the deleted code contains active statements.
                                        // TODO (https://github.com/dotnet/roslyn/issues/51177):
                                        // Only report rude edit when replacing member with an implicit one if it has an active statement.
                                        // We might be able to support moving active members but we would need to 
                                        // 1) Move AnalyzeChangedMemberBody from Insert to Delete
                                        // 2) Handle active statements that moved to a different document in ActiveStatementTrackingService
                                        // 3) The debugger's ManagedActiveStatementUpdate might need another field indicating the source file path.
                                        if (hasActiveStatement)
                                        {
                                            ReportDeletedMemberRudeEdit(diagnostics, oldSymbol, newCompilation, RudeEditKind.DeleteActiveStatement, cancellationToken);
                                            continue;
                                        }

                                        if (!newSymbol.IsImplicitlyDeclared)
                                        {
                                            // Ignore the delete. The new symbol is explicitly declared and thus there will be an insert edit that will issue a semantic update.
                                            // Note that this could also be the case for deleting properties of records, but they will be handled when we see
                                            // their accessors below.
                                            continue;
                                        }

                                        if (IsPropertyAccessorDeclarationMatchingPrimaryConstructorParameter(oldDeclaration, newSymbol.ContainingType, out var isFirst))
                                        {
                                            // Defer a constructor edit to cover the property initializer changing
                                            DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration: null, syntaxMap, oldSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

                                            // If there was no body deleted then we are done since the compiler generated property also has no body
                                            if (TryGetDeclarationBody(oldDeclaration) is null)
                                            {
                                                continue;
                                            }

                                            // If there was a body, then the backing field of the property will be affected so we
                                            // need to issue edits for the synthezied members.
                                            // We only need to do this once though.
                                            if (isFirst)
                                            {
                                                AddEditsForSynthesizedRecordMembers(newCompilation, newSymbol.ContainingType, semanticEdits, cancellationToken);
                                            }
                                        }

                                        // If a constructor is deleted and replaced by an implicit one the update needs to aggregate updates to all data member initializers,
                                        // or if a property is deleted that is part of a records primary constructor, which is effectivelly moving from an explicit to implicit
                                        // initializer.
                                        if (IsConstructorWithMemberInitializers(oldDeclaration))
                                        {
                                            processedSymbols.Remove(oldSymbol);
                                            DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration: null, syntaxMap, oldSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);
                                            continue;
                                        }

                                        // there is no insert edit for an implicit declaration, therefore we need to issue an update:
                                        editKind = SemanticEditKind.Update;
                                    }
                                    else
                                    {
                                        var diagnosticSpan = GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, oldDeclaration);

                                        // If we got here for a global statement then the actual edit is a delete of the synthesized Main method
                                        if (IsGlobalMain(oldSymbol))
                                        {
                                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.Delete, diagnosticSpan, edit.OldNode, new[] { GetDisplayName(edit.OldNode, EditKind.Delete) }));
                                            continue;
                                        }

                                        // If the associated member declaration (accessor -> property/indexer/event, parameter -> method) has also been deleted skip
                                        // the delete of the symbol as it will be deleted by the delete of the associated member.
                                        //
                                        // Associated member declarations must be in the same document as the symbol, so we don't need to resolve their symbol.
                                        // In some cases the symbol even can't be resolved unambiguously. Consider e.g. resolving a method with its parameter deleted -
                                        // we wouldn't know which overload to resolve to.
                                        if (TryGetAssociatedMemberDeclaration(oldDeclaration, out var oldAssociatedMemberDeclaration))
                                        {
                                            if (HasEdit(editMap, oldAssociatedMemberDeclaration, EditKind.Delete))
                                            {
                                                continue;
                                            }
                                        }
                                        else if (oldSymbol.ContainingType != null)
                                        {
                                            // Check if the symbol being deleted is a member of a type that's also being deleted.
                                            // If so, skip the member deletion and only report the containing symbol deletion.
                                            var containingSymbolKey = SymbolKey.Create(oldSymbol.ContainingType, cancellationToken);
                                            var newContainingSymbol = containingSymbolKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                                            if (newContainingSymbol == null)
                                            {
                                                continue;
                                            }
                                        }

                                        // deleting symbol is not allowed

                                        diagnostics.Add(new RudeEditDiagnostic(
                                            RudeEditKind.Delete,
                                            diagnosticSpan,
                                            oldDeclaration,
                                            new[]
                                            {
                                                string.Format(FeaturesResources.member_kind_and_name,
                                                    GetDisplayName(oldDeclaration, EditKind.Delete),
                                                    oldSymbol.ToDisplayString(diagnosticSpan.IsEmpty ? s_fullyQualifiedMemberDisplayFormat : s_unqualifiedMemberDisplayFormat))
                                            }));

                                        continue;
                                    }
                                }

                                break;

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
                                        // Symbol has actually not been inserted but rather moved between documents or partial type declarations,
                                        // or is replacing an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)
                                        oldContainingType = oldSymbol.ContainingType;

                                        if (oldSymbol.IsImplicitlyDeclared)
                                        {
                                            // If a user explicitly implements a member of a record then we want to issue an update, not an insert.
                                            if (oldSymbol.DeclaringSyntaxReferences.Length == 1)
                                            {
                                                Contract.ThrowIfNull(oldDeclaration);
                                                ReportDeclarationInsertDeleteRudeEdits(diagnostics, oldDeclaration, newDeclaration, oldSymbol, newSymbol, capabilities, cancellationToken);

                                                if (IsPropertyAccessorDeclarationMatchingPrimaryConstructorParameter(newDeclaration, newContainingType, out var isFirst))
                                                {
                                                    // If there is no body declared we can skip it entirely because for a property accessor
                                                    // it matches what the compiler would have previously implicitly implemented.
                                                    if (TryGetDeclarationBody(newDeclaration) is null)
                                                    {
                                                        continue;
                                                    }

                                                    // If there was a body, then the backing field of the property will be affected so we
                                                    // need to issue edits for the synthezied members. Only need to do it once.
                                                    if (isFirst)
                                                    {
                                                        AddEditsForSynthesizedRecordMembers(newCompilation, newContainingType, semanticEdits, cancellationToken);
                                                    }
                                                }

                                                editKind = SemanticEditKind.Update;
                                            }
                                        }
                                        else if (oldSymbol.DeclaringSyntaxReferences.Length == 1 && newSymbol.DeclaringSyntaxReferences.Length == 1)
                                        {
                                            Contract.ThrowIfNull(oldDeclaration);

                                            // Handles partial methods and explicitly implemented properties that implement positional parameters of records

                                            // We ignore partial method definition parts when processing edits (GetSymbolForEdit).
                                            // The only declaration in compilation without syntax errors that can have multiple declaring references is a type declaration.
                                            // We can therefore ignore any symbols that have more than one declaration.
                                            ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newDeclaration, newModel, ref lazyLayoutAttribute);

                                            // Compare the old declaration syntax of the symbol with its new declaration and report rude edits
                                            // if it changed in any way that's not allowed.
                                            ReportDeclarationInsertDeleteRudeEdits(diagnostics, oldDeclaration, newDeclaration, oldSymbol, newSymbol, capabilities, cancellationToken);

                                            var oldBody = TryGetDeclarationBody(oldDeclaration);
                                            if (oldBody != null)
                                            {
                                                // The old symbol's declaration syntax may be located in a different document than the old version of the current document.
                                                var oldSyntaxDocument = oldProject.Solution.GetRequiredDocument(oldDeclaration.SyntaxTree);
                                                var oldSyntaxModel = await oldSyntaxDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                                                var oldSyntaxText = await oldSyntaxDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
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
                                            var isNewConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newDeclaration);
                                            var isDeclarationWithInitializer = IsDeclarationWithInitializer(oldDeclaration) || IsDeclarationWithInitializer(newDeclaration);
                                            var isRecordPrimaryConstructorParameter = IsRecordPrimaryConstructorParameter(oldDeclaration);

                                            if (isNewConstructorWithMemberInitializers || isDeclarationWithInitializer || isRecordPrimaryConstructorParameter)
                                            {
                                                if (isNewConstructorWithMemberInitializers)
                                                {
                                                    processedSymbols.Remove(newSymbol);
                                                }

                                                if (isDeclarationWithInitializer)
                                                {
                                                    AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newCompilation, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, cancellationToken);
                                                }

                                                DeferConstructorEdit(oldSymbol.ContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

                                                // Don't add a separate semantic edit.
                                                // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                                continue;
                                            }

                                            editKind = SemanticEditKind.Update;
                                        }
                                        else
                                        {
                                            editKind = SemanticEditKind.Update;
                                        }
                                    }
                                    else if (TryGetAssociatedMemberDeclaration(newDeclaration, out var newAssociatedMemberDeclaration) &&
                                             HasEdit(editMap, newAssociatedMemberDeclaration, EditKind.Insert))
                                    {
                                        // If the symbol is an accessor and the containing property/indexer/event declaration has also been inserted skip
                                        // the insert of the accessor as it will be inserted by the property/indexer/event.
                                        continue;
                                    }
                                    else if (newSymbol is IParameterSymbol or ITypeParameterSymbol)
                                    {
                                        diagnostics.Add(new RudeEditDiagnostic(
                                            RudeEditKind.Insert,
                                            GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                            newDeclaration,
                                            arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));

                                        continue;
                                    }
                                    else if (newContainingType != null && !IsGlobalMain(newSymbol))
                                    {
                                        // The edit actually adds a new symbol into an existing or a new type.

                                        var containingSymbolKey = SymbolKey.Create(newContainingType, cancellationToken);
                                        oldContainingType = containingSymbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol as INamedTypeSymbol;

                                        if (oldContainingType != null && !CanAddNewMember(newSymbol, capabilities))
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

                                        // If a property or field is added to a record then the implicit constructors change,
                                        // and we need to mark a number of other synthesized members as having changed.
                                        if (newSymbol is IPropertySymbol or IFieldSymbol && newContainingType.IsRecord)
                                        {
                                            DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

                                            AddEditsForSynthesizedRecordMembers(newCompilation, newContainingType, semanticEdits, cancellationToken);
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

                                    var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newDeclaration);
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

                                        DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

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
                                    var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newDeclaration);
                                    var isDeclarationWithInitializer = IsDeclarationWithInitializer(oldDeclaration) || IsDeclarationWithInitializer(newDeclaration);

                                    if (isConstructorWithMemberInitializers || isDeclarationWithInitializer)
                                    {
                                        if (isConstructorWithMemberInitializers)
                                        {
                                            processedSymbols.Remove(newSymbol);
                                        }

                                        if (isDeclarationWithInitializer)
                                        {
                                            AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newCompilation, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, cancellationToken);
                                        }

                                        DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

                                        // Don't add a separate semantic edit.
                                        // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                        continue;
                                    }
                                }

                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                        }

                        Contract.ThrowIfFalse(editKind is SemanticEditKind.Update or SemanticEditKind.Insert);

                        if (editKind == SemanticEditKind.Update)
                        {
                            Contract.ThrowIfNull(oldSymbol);

                            AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newCompilation, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, cancellationToken);
                            if (newSymbol is INamedTypeSymbol or IFieldSymbol or IPropertySymbol or IEventSymbol or IParameterSymbol or ITypeParameterSymbol)
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
                        var isActiveMember =
                            GetOverlappingActiveStatements(oldDeclaration, oldActiveStatements).Any() ||
                            IsStateMachineMethod(oldDeclaration) ||
                            ContainsLambda(oldDeclaration);

                        var syntaxMap = isActiveMember ? CreateSyntaxMapForEquivalentNodes(oldDeclaration, newDeclaration) : null;

                        // only trivia changed:
                        Contract.ThrowIfFalse(IsConstructorWithMemberInitializers(oldDeclaration) == IsConstructorWithMemberInitializers(newDeclaration));
                        Contract.ThrowIfFalse(IsDeclarationWithInitializer(oldDeclaration) == IsDeclarationWithInitializer(newDeclaration));

                        var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newDeclaration);
                        var isDeclarationWithInitializer = IsDeclarationWithInitializer(newDeclaration);

                        if (isConstructorWithMemberInitializers || isDeclarationWithInitializer)
                        {
                            // TODO: only create syntax map if any field initializers are active/contain lambdas or this is a partial type
                            syntaxMap ??= CreateSyntaxMapForEquivalentNodes(oldDeclaration, newDeclaration);

                            if (isConstructorWithMemberInitializers)
                            {
                                processedSymbols.Remove(newSymbol);
                            }

                            DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, ref instanceConstructorEdits, ref staticConstructorEdits);

                            // Don't add a separate semantic edit.
                            // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                            continue;
                        }

                        ReportMemberBodyUpdateRudeEdits(diagnostics, newDeclaration, diagnosticSpan);

                        // updating generic methods and types
                        if (InGenericContext(oldSymbol, out var oldIsGenericMethod))
                        {
                            var rudeEdit = oldIsGenericMethod ? RudeEditKind.GenericMethodTriviaUpdate : RudeEditKind.GenericTypeTriviaUpdate;
                            diagnostics.Add(new RudeEditDiagnostic(rudeEdit, diagnosticSpan, newEditNode, new[] { GetDisplayName(newEditNode) }));
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
                        newCompilation,
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
                        newCompilation,
                        processedSymbols,
                        capabilities,
                        isStatic: true,
                        semanticEdits,
                        diagnostics,
                        cancellationToken);
                }
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
                        GetSymbolDeclarationSyntax(oldSymbol.DeclaringSyntaxReferences.Single(), cancellationToken) : oldNode,
                    (newSymbol != null && newSymbol.DeclaringSyntaxReferences.Length == 1) ?
                        GetSymbolDeclarationSyntax(newSymbol.DeclaringSyntaxReferences.Single(), cancellationToken) : newNode);
            }
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
                => s_symbolKeyComparer.Equals(x.Symbol, y.Symbol);

            public int GetHashCode([DisallowNull] SemanticEditInfo obj)
                => obj.Symbol.GetHashCode();
        }

        private void ReportUpdatedSymbolDeclarationRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            Compilation newCompilation,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasGeneratedAttributeChange,
            out bool hasGeneratedReturnTypeAttributeChange,
            out bool hasParameterRename,
            CancellationToken cancellationToken)
        {
            var rudeEdit = RudeEditKind.None;

            hasGeneratedAttributeChange = false;
            hasGeneratedReturnTypeAttributeChange = false;
            hasParameterRename = false;

            if (oldSymbol.Kind != newSymbol.Kind)
            {
                rudeEdit = (oldSymbol.Kind == SymbolKind.Field || newSymbol.Kind == SymbolKind.Field) ? RudeEditKind.FieldKindUpdate : RudeEditKind.Update;
            }
            else if (oldSymbol.Name != newSymbol.Name)
            {
                if (oldSymbol is IParameterSymbol && newSymbol is IParameterSymbol)
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
                else
                {
                    rudeEdit = RudeEditKind.Renamed;
                }

                // specialize rude edit for accessors and conversion operators:
                if (oldSymbol is IMethodSymbol oldMethod && newSymbol is IMethodSymbol newMethod)
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
                if (!oldMethod.ExplicitInterfaceImplementations.SequenceEqual(newMethod.ExplicitInterfaceImplementations, (x, y) => SymbolsEquivalent(x, y)))
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
                    AnalyzeReturnType(oldMethod, newMethod, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange);
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
                        AnalyzeReturnType(oldType.DelegateInvokeMethod, newType.DelegateInvokeMethod, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange);
                    }
                }
            }
            else if (oldSymbol is IPropertySymbol oldProperty && newSymbol is IPropertySymbol newProperty)
            {
                AnalyzeReturnType(oldProperty, newProperty, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange);
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
                    AnalyzeReturnType(oldEvent, newEvent, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange);
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
                    AnalyzeParameterType(oldParameter, newParameter, ref rudeEdit, ref hasGeneratedAttributeChange);
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

            if (rudeEdit != RudeEditKind.None)
            {
                // so we'll just use the last global statement in the file

                ReportUpdateRudeEdit(diagnostics, rudeEdit, oldSymbol, newSymbol, newNode, newCompilation, cancellationToken);
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

        private static void AnalyzeParameterType(IParameterSymbol oldParameter, IParameterSymbol newParameter, ref RudeEditKind rudeEdit, ref bool hasGeneratedAttributeChange)
        {
            if (!ParameterTypesEquivalent(oldParameter, newParameter, exact: true))
            {
                if (ParameterTypesEquivalent(oldParameter, newParameter, exact: false))
                {
                    hasGeneratedAttributeChange = true;
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

        private static void AnalyzeReturnType(IMethodSymbol oldMethod, IMethodSymbol newMethod, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange)
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
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private static void AnalyzeReturnType(IEventSymbol oldEvent, IEventSymbol newEvent, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange)
        {
            if (!ReturnTypesEquivalent(oldEvent, newEvent, exact: true))
            {
                if (ReturnTypesEquivalent(oldEvent, newEvent, exact: false))
                {
                    hasGeneratedReturnTypeAttributeChange = true;
                }
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private static void AnalyzeReturnType(IPropertySymbol oldProperty, IPropertySymbol newProperty, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange)
        {
            if (!ReturnTypesEquivalent(oldProperty, newProperty, exact: true))
            {
                if (ReturnTypesEquivalent(oldProperty, newProperty, exact: false))
                {
                    hasGeneratedReturnTypeAttributeChange = true;
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
            Compilation newCompilation,
            Match<SyntaxNode> topMatch,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            CancellationToken cancellationToken)
        {
            // TODO: fails in VB on delegate parameter https://github.com/dotnet/roslyn/issues/53337
            // Contract.ThrowIfFalse(newSymbol.IsImplicitlyDeclared == newDeclaration is null);

            ReportCustomAttributeRudeEdits(diagnostics, oldSymbol, newSymbol, newNode, newCompilation, capabilities, out var hasAttributeChange, out var hasReturnTypeAttributeChange, cancellationToken);

            ReportUpdatedSymbolDeclarationRudeEdits(diagnostics, oldSymbol, newSymbol, newNode, newCompilation, capabilities, out var hasGeneratedAttributeChange, out var hasGeneratedReturnTypeAttributeChange, out var hasParameterRename, cancellationToken);
            hasAttributeChange |= hasGeneratedAttributeChange;
            hasReturnTypeAttributeChange |= hasGeneratedReturnTypeAttributeChange;

            if (hasParameterRename)
            {
                Debug.Assert(newSymbol is IParameterSymbol);
                AddParameterUpdateSemanticEdit(semanticEdits, (IParameterSymbol)newSymbol, syntaxMap, cancellationToken);
            }
            else if (hasAttributeChange || hasReturnTypeAttributeChange)
            {
                AddCustomAttributeSemanticEdits(semanticEdits, oldSymbol, newSymbol, topMatch, syntaxMap, hasAttributeChange, hasReturnTypeAttributeChange, cancellationToken);
            }

            // updating generic methods and types
            if (InGenericContext(oldSymbol, out var oldIsGenericMethod) || InGenericContext(newSymbol, out _))
            {
                var rudeEdit = oldIsGenericMethod ? RudeEditKind.GenericMethodUpdate : RudeEditKind.GenericTypeUpdate;
                ReportUpdateRudeEdit(diagnostics, rudeEdit, newSymbol, newNode, cancellationToken);
            }
        }

        private static void AddCustomAttributeSemanticEdits(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            Match<SyntaxNode> topMatch,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
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
                AddParameterUpdateSemanticEdit(semanticEdits, newParameterSymbol, syntaxMap, cancellationToken);
            }
        }

        private static void AddParameterUpdateSemanticEdit(ArrayBuilder<SemanticEditInfo> semanticEdits, IParameterSymbol newParameterSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, CancellationToken cancellationToken)
        {
            var newContainingSymbol = newParameterSymbol.ContainingSymbol;
            semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(newContainingSymbol, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));

            // attributes applied on parameters of a delegate are applied to both Invoke and BeginInvoke methods
            if (newContainingSymbol.ContainingSymbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate } newContainingDelegateType)
            {
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
            Compilation newCompilation,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasAttributeChange,
            out bool hasReturnTypeAttributeChange,
            CancellationToken cancellationToken)
        {
            // This is the only case we care about whether to issue an edit or not, because this is the only case where types have their attributes checked
            // and types are the only things that would otherwise not have edits reported.
            hasAttributeChange = ReportCustomAttributeRudeEdits(diagnostics, oldSymbol.GetAttributes(), newSymbol.GetAttributes(), oldSymbol, newSymbol, newNode, newCompilation, capabilities, cancellationToken);

            hasReturnTypeAttributeChange = false;

            if (oldSymbol is IMethodSymbol oldMethod &&
                newSymbol is IMethodSymbol newMethod)
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnostics, oldMethod.GetReturnTypeAttributes(), newMethod.GetReturnTypeAttributes(), oldSymbol, newSymbol, newNode, newCompilation, capabilities, cancellationToken);
            }
            else if (oldSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var oldInvokeMethod } &&
                     newSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var newInvokeMethod })
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnostics, oldInvokeMethod.GetReturnTypeAttributes(), newInvokeMethod.GetReturnTypeAttributes(), oldSymbol, newSymbol, newNode, newCompilation, capabilities, cancellationToken);
            }
        }

        private bool ReportCustomAttributeRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ImmutableArray<AttributeData>? oldAttributes,
            ImmutableArray<AttributeData> newAttributes,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            Compilation newCompilation,
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
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingAttributesNotSupportedByRuntime, oldSymbol, newSymbol, newNode, newCompilation, cancellationToken);
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

        private static bool CanAddNewMember(ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities)
        {
            if (newSymbol is IMethodSymbol or IPropertySymbol) // Properties are just get_ and set_ methods
            {
                return capabilities.Grant(EditAndContinueCapabilities.AddMethodToExistingType);
            }
            else if (newSymbol is IFieldSymbol field)
            {
                if (field.IsStatic)
                {
                    return capabilities.Grant(EditAndContinueCapabilities.AddStaticFieldToExistingType);
                }

                return capabilities.Grant(EditAndContinueCapabilities.AddInstanceFieldToExistingType);
            }

            return true;
        }

        private static void AddEditsForSynthesizedRecordMembers(Compilation compilation, INamedTypeSymbol recordType, ArrayBuilder<SemanticEditInfo> semanticEdits, CancellationToken cancellationToken)
        {
            foreach (var member in GetRecordUpdatedSynthesizedMembers(compilation, recordType))
            {
                var symbolKey = SymbolKey.Create(member, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, symbolKey, syntaxMap: null, syntaxMapTree: null, partialType: null));
            }
        }

        private static IEnumerable<ISymbol> GetRecordUpdatedSynthesizedMembers(Compilation compilation, INamedTypeSymbol record)
        {
            // All methods that are updated have well known names, and calling GetMembers(string) is
            // faster than enumerating.

            // When a new field or property is added the PrintMembers, Equals(R) and GetHashCode() methods are updated
            // We don't need to worry about Deconstruct because it only changes when a new positional parameter
            // is added, and those are rude edits (due to adding a constructor parameter).
            // We don't need to worry about the constructors as they are reported elsewhere.
            // We have to use SingleOrDefault and check IsImplicitlyDeclared because the user can provide their
            // own implementation of these methods, and edits to them are handled by normal processing.
            var result = record.GetMembers(WellKnownMemberNames.PrintMembersMethodName)
                .OfType<IMethodSymbol>()
                .SingleOrDefault(m =>
                    m.IsImplicitlyDeclared &&
                    m.Parameters.Length == 1 &&
                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, compilation.GetTypeByMetadataName(typeof(StringBuilder).FullName!)) &&
                    SymbolEqualityComparer.Default.Equals(m.ReturnType, compilation.GetTypeByMetadataName(typeof(bool).FullName!)));
            if (result is not null)
            {
                yield return result;
            }

            result = record.GetMembers(WellKnownMemberNames.ObjectEquals)
                .OfType<IMethodSymbol>()
                .SingleOrDefault(m =>
                    m.IsImplicitlyDeclared &&
                    m.Parameters.Length == 1 &&
                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType));
            if (result is not null)
            {
                yield return result;
            }

            result = record.GetMembers(WellKnownMemberNames.ObjectGetHashCode)
               .OfType<IMethodSymbol>()
               .SingleOrDefault(m =>
                    m.IsImplicitlyDeclared &&
                    m.Parameters.Length == 0);
            if (result is not null)
            {
                yield return result;
            }
        }

        private void ReportDeletedMemberRudeEdit(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol oldSymbol,
            Compilation newCompilation,
            RudeEditKind rudeEditKind,
            CancellationToken cancellationToken)
        {
            var newNode = GetDeleteRudeEditDiagnosticNode(oldSymbol, newCompilation, cancellationToken);

            diagnostics.Add(new RudeEditDiagnostic(
                rudeEditKind,
                GetDiagnosticSpan(newNode, EditKind.Delete),
                arguments: new[]
                {
                    string.Format(FeaturesResources.member_kind_and_name, GetDisplayName(oldSymbol), oldSymbol.ToDisplayString(s_unqualifiedMemberDisplayFormat))
                }));
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
            var span = (rudeEdit == RudeEditKind.ChangeImplicitMainReturnType) ? GetGlobalStatementDiagnosticSpan(node) : GetDiagnosticSpan(node, EditKind.Update);

            var arguments = rudeEdit switch
            {
                RudeEditKind.TypeKindUpdate or
                RudeEditKind.ChangeImplicitMainReturnType or
                RudeEditKind.GenericMethodUpdate or
                RudeEditKind.GenericTypeUpdate
                    => Array.Empty<string>(),

                RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime
                    => new[] { CreateNewOnMetadataUpdateAttributeName },

                _ => new[] { GetDisplayName(newSymbol) }
            };

            diagnostics.Add(new RudeEditDiagnostic(rudeEdit, span, node, arguments));
        }

        private void ReportUpdateRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, RudeEditKind rudeEdit, ISymbol oldSymbol, ISymbol newSymbol, SyntaxNode? newNode, Compilation newCompilation, CancellationToken cancellationToken)
        {
            if (newSymbol.IsImplicitlyDeclared)
            {
                ReportDeletedMemberRudeEdit(diagnostics, oldSymbol, newCompilation, rudeEdit, cancellationToken);
            }
            else
            {
                ReportUpdateRudeEdit(diagnostics, rudeEdit, newSymbol, newNode, cancellationToken);
            }
        }

        private static SyntaxNode GetRudeEditDiagnosticNode(ISymbol symbol, CancellationToken cancellationToken)
        {
            var container = symbol;
            while (container != null)
            {
                if (container.DeclaringSyntaxReferences.Length > 0)
                {
                    return container.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                }

                container = container.ContainingSymbol;
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static SyntaxNode GetDeleteRudeEditDiagnosticNode(ISymbol oldSymbol, Compilation newCompilation, CancellationToken cancellationToken)
        {
            var oldContainer = oldSymbol.ContainingSymbol;
            while (oldContainer != null)
            {
                var containerKey = SymbolKey.Create(oldContainer, cancellationToken);
                var newContainer = containerKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                if (newContainer != null)
                {
                    return GetRudeEditDiagnosticNode(newContainer, cancellationToken);
                }

                oldContainer = oldContainer.ContainingSymbol;
            }

            throw ExceptionUtilities.Unreachable;
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

        private Func<SyntaxNode, SyntaxNode?> CreateSyntaxMapForEquivalentNodes(SyntaxNode oldDeclaration, SyntaxNode newDeclaration)
        {
            return newNode => newDeclaration.FullSpan.Contains(newNode.SpanStart) ?
                FindDeclarationBodyPartner(newDeclaration, oldDeclaration, newNode) : null;
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
                        return FindDeclarationBodyPartner(newDeclaration, oldDeclaration, newNode);
                    }
                }

                return null;
            };
        }

        #region Constructors and Initializers

        /// <summary>
        /// Called when a body of a constructor or an initializer of a member is updated or inserted.
        /// </summary>
        private static void DeferConstructorEdit(
            INamedTypeSymbol oldType,
            INamedTypeSymbol newType,
            SyntaxNode? newDeclaration,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            bool isStatic,
            ref PooledDictionary<INamedTypeSymbol, ConstructorEdit>? instanceConstructorEdits,
            ref PooledDictionary<INamedTypeSymbol, ConstructorEdit>? staticConstructorEdits)
        {
            Dictionary<INamedTypeSymbol, ConstructorEdit> constructorEdits;
            if (isStatic)
            {
                constructorEdits = staticConstructorEdits ??= PooledDictionary<INamedTypeSymbol, ConstructorEdit>.GetInstance();
            }
            else
            {
                constructorEdits = instanceConstructorEdits ??= PooledDictionary<INamedTypeSymbol, ConstructorEdit>.GetInstance();
            }

            if (!constructorEdits.TryGetValue(newType, out var constructorEdit))
            {
                constructorEdits.Add(newType, constructorEdit = new ConstructorEdit(oldType));
            }

            if (newDeclaration != null && !constructorEdit.ChangedDeclarations.ContainsKey(newDeclaration))
            {
                constructorEdit.ChangedDeclarations.Add(newDeclaration, syntaxMap);
            }
        }

        private void AddConstructorEdits(
            IReadOnlyDictionary<INamedTypeSymbol, ConstructorEdit> updatedTypes,
            Match<SyntaxNode> topMatch,
            SemanticModel? oldModel,
            Compilation oldCompilation,
            Compilation newCompilation,
            IReadOnlySet<ISymbol> processedSymbols,
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

                var anyInitializerUpdatesInCurrentDocument = updatesInCurrentDocument.ChangedDeclarations.Keys.Any(IsDeclarationWithInitializer);
                var isPartialEdit = IsPartialEdit(oldType, newType, oldSyntaxTree, newSyntaxTree);

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

                    var newCtorKey = SymbolKey.Create(newCtor, cancellationToken);

                    var syntaxMapToUse = aggregateSyntaxMap;

                    SyntaxNode? newDeclaration = null;
                    ISymbol? oldCtor;
                    if (!newCtor.IsImplicitlyDeclared)
                    {
                        // Constructors have to have a single declaration syntax, they can't be partial
                        newDeclaration = GetSymbolDeclarationSyntax(newCtor.DeclaringSyntaxReferences.Single(), cancellationToken);

                        // Implicit record constructors are represented by the record declaration itself.
                        // https://github.com/dotnet/roslyn/issues/54403
                        var isPrimaryRecordConstructor = IsRecordDeclaration(newDeclaration);

                        // Constructor that doesn't contain initializers had a corresponding semantic edit produced previously 
                        // or was not edited. In either case we should not produce a semantic edit for it.
                        if (!isPrimaryRecordConstructor && !IsConstructorWithMemberInitializers(newDeclaration))
                        {
                            continue;
                        }

                        // If no initializer updates were made in the type we only need to produce semantic edits for constructors
                        // whose body has been updated, otherwise we need to produce edits for all constructors that include initializers.
                        // If changes were made to initializers or constructors of a partial type in another document they will be merged
                        // when aggregating semantic edits from all changed documents. Rude edits resulting from those changes, if any, will
                        // be reported in the document they were made in.
                        if (!isPrimaryRecordConstructor && !anyInitializerUpdatesInCurrentDocument && !updatesInCurrentDocument.ChangedDeclarations.ContainsKey(newDeclaration))
                        {
                            continue;
                        }

                        // To avoid costly SymbolKey resolution we first try to match the constructor in the current document
                        // and special case parameter-less constructor.

                        // In the case of records, newDeclaration will point to the record declaration, take the slow path.
                        if (!isPrimaryRecordConstructor && topMatch.TryGetOldNode(newDeclaration, out var oldDeclaration))
                        {
                            Contract.ThrowIfNull(oldModel);
                            oldCtor = oldModel.GetDeclaredSymbol(oldDeclaration, cancellationToken);
                            Contract.ThrowIfFalse(oldCtor is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor });
                        }
                        else if (!isPrimaryRecordConstructor && newCtor.Parameters.Length == 0)
                        {
                            oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                        }
                        else
                        {
                            var resolution = newCtorKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken);

                            // There may be semantic errors in the compilation that result in multiple candidates.
                            // Pick the first candidate.

                            oldCtor = resolution.Symbol;
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
                            // attribute rude edit to one of the modified members
                            var firstSpan = updatesInCurrentDocument.ChangedDeclarations.Keys.Where(IsDeclarationWithInitializer).Aggregate(
                                (min: int.MaxValue, span: default(TextSpan)),
                                (accumulate, node) => (node.SpanStart < accumulate.min) ? (node.SpanStart, node.Span) : accumulate).span;

                            Contract.ThrowIfTrue(firstSpan.IsEmpty);
                            ReportMemberBodyUpdateRudeEdits(diagnostics, newDeclaration, firstSpan);
                        }

                        // When explicitly implementing the copy constructor of a record the parameter name if the runtime doesn't support
                        // updating parameters, otherwise the debugger would show the incorrect name in the autos/locals/watch window
                        if (oldCtor != null &&
                            !isPrimaryRecordConstructor &&
                            oldCtor.DeclaringSyntaxReferences.Length == 0 &&
                            newCtor.Parameters.Length == 1 &&
                            newType.IsRecord &&
                            oldCtor.GetParameters().First().Name != newCtor.GetParameters().First().Name &&
                            !capabilities.Grant(EditAndContinueCapabilities.UpdateParameters))
                        {
                            diagnostics.Add(new RudeEditDiagnostic(
                                RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch,
                                GetDiagnosticSpan(newDeclaration, EditKind.Update),
                                arguments: new[] { oldCtor.ToDisplayString(SymbolDisplayFormats.NameFormat) }));

                            continue;
                        }
                    }
                    else
                    {
                        if (newCtor.Parameters.Length == 1)
                        {
                            // New constructor is implicitly declared with a parameter, so its the copy constructor of a record
                            Debug.Assert(oldType.IsRecord);
                            Debug.Assert(newType.IsRecord);

                            // We only need an edit for this if the number of properties or fields on the record has changed. Changes to
                            // initializers, or whether the property is part of the primary constructor, will still come through this code
                            // path because they need an edit to the other constructor, but not the copy construcor.
                            if (oldType.GetMembers().OfType<IPropertySymbol>().Count() == newType.GetMembers().OfType<IPropertySymbol>().Count() &&
                                oldType.GetMembers().OfType<IFieldSymbol>().Count() == newType.GetMembers().OfType<IFieldSymbol>().Count())
                            {
                                continue;
                            }

                            oldCtor = oldType.InstanceConstructors.Single(c => c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, c.ContainingType));
                            // The copy constructor does not have a syntax map
                            syntaxMapToUse = null;
                            // Since there is no syntax map, we don't need to handle anything special to merge them for partial types.
                            // The easiest way to do this is just to pretend this isn't a partial edit.
                            isPartialEdit = false;
                        }
                        else
                        {
                            // New constructor is implicitly declared so it must be parameterless.
                            //
                            // Instance constructor:
                            //   Its presence indicates there are no other instance constructors in the new type and therefore
                            //   there must be a single parameterless instance constructor in the old type (constructors with parameters can't be removed).
                            //
                            // Static constructor:
                            //    Static constructor is always parameterless and not implicitly generated if there are no static initializers.
                            oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                        }

                        Contract.ThrowIfFalse(isStatic || oldCtor != null);
                    }

                    if (oldCtor != null)
                    {
                        AnalyzeSymbolUpdate(oldCtor, newCtor, newDeclaration, newCompilation, topMatch, capabilities, diagnostics, semanticEdits, syntaxMapToUse, cancellationToken);

                        semanticEdits.Add(new SemanticEditInfo(
                            SemanticEditKind.Update,
                            newCtorKey,
                            syntaxMapToUse,
                            syntaxMapTree: isPartialEdit ? newSyntaxTree : null,
                            partialType: isPartialEdit ? SymbolKey.Create(newType, cancellationToken) : null));
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
                }
            }
        }

        private bool HasMemberInitializerContainingLambda(INamedTypeSymbol type, bool isStatic, ref bool? lazyHasMemberInitializerContainingLambda, CancellationToken cancellationToken)
        {
            if (lazyHasMemberInitializerContainingLambda == null)
            {
                // checking the old type for existing lambdas (it's ok for the new initializers to contain lambdas)
                lazyHasMemberInitializerContainingLambda = HasMemberInitializerContainingLambda(type, isStatic, cancellationToken);
            }

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
                    var syntax = GetSymbolDeclarationSyntax(member.DeclaringSyntaxReferences.Single(), cancellationToken);
                    if (IsDeclarationWithInitializer(syntax) && ContainsLambda(syntax))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ISymbol? TryGetParameterlessConstructor(INamedTypeSymbol type, bool isStatic)
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
            SyntaxNode oldMemberBody,
            SemanticModel newModel,
            SyntaxNode newMemberBody,
            ISymbol newMember,
            IReadOnlyDictionary<SyntaxNode, LambdaInfo>? matchedLambdas,
            BidirectionalMap<SyntaxNode> map,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool syntaxMapRequired,
            CancellationToken cancellationToken)
        {
            syntaxMapRequired = false;

            if (matchedLambdas != null)
            {
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
            var oldHasLambdas = oldLambdaBodyEnumerator.MoveNext();
            var newHasLambdas = newLambdaBodyEnumerator.MoveNext();

            // Exit early if there are no lambdas in the method to avoid expensive data flow analysis:
            if (!oldHasLambdas && !newHasLambdas)
            {
                return;
            }

            var oldCaptures = GetCapturedVariables(oldModel, oldMemberBody);
            var newCaptures = GetCapturedVariables(newModel, newMemberBody);

            // { new capture index -> old capture index }
            using var _1 = ArrayBuilder<int>.GetInstance(newCaptures.Length, fillWithValue: 0, out var reverseCapturesMap);

            // { new capture index -> new closure scope or null for "this" }
            using var _2 = ArrayBuilder<SyntaxNode?>.GetInstance(newCaptures.Length, fillWithValue: null, out var newCapturesToClosureScopes);

            // Can be calculated from other maps but it's simpler to just calculate it upfront.
            // { old capture index -> old closure scope or null for "this" }
            using var _3 = ArrayBuilder<SyntaxNode?>.GetInstance(oldCaptures.Length, fillWithValue: null, out var oldCapturesToClosureScopes);

            CalculateCapturedVariablesMaps(
                oldCaptures,
                oldMemberBody,
                newCaptures,
                newMember,
                newMemberBody,
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
                            var arguments = new[] { newCapture.Name, GetDisplayName(GetLambda(newLambdaBody)) };

                            if (newCapture.IsThisParameter() || oldAccessed)
                            {
                                // changed accessed to "this", or captured variable accessed in old lambda is not accessed in the new lambda
                                diagnostics.Add(new RudeEditDiagnostic(rudeEdit, GetDiagnosticSpan(GetLambda(newLambdaBody), EditKind.Update), null, arguments));
                            }
                            else if (newAccessed)
                            {
                                // captured variable accessed in new lambda is not accessed in the old lambda
                                var hasUseSites = false;
                                foreach (var useSite in GetVariableUseSites(GetLambdaBodyExpressionsAndStatements(newLambdaBody), newCapture, newModel, cancellationToken))
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

            var containingTypeDeclaration = TryGetContainingTypeDeclaration(newMemberBody);
            var isInInterfaceDeclaration = containingTypeDeclaration != null && IsInterfaceDeclaration(containingTypeDeclaration);

            var newHasLambdaBodies = newHasLambdas;
            while (newHasLambdaBodies)
            {
                var (newLambda, newLambdaBody1, newLambdaBody2) = newLambdaBodyEnumerator.Current;

                if (!map.Reverse.ContainsKey(newLambda))
                {
                    if (!CanAddNewLambda(newLambda, capabilities, matchedLambdas))
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

            // Similarly for addition. We don't allow removal of lambda that has captures from multiple scopes.

            var oldHasMoreLambdas = oldHasLambdas;
            while (oldHasMoreLambdas)
            {
                var (oldLambda, oldLambdaBody1, oldLambdaBody2) = oldLambdaBodyEnumerator.Current;

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

            syntaxMapRequired = newHasLambdas;
        }

        private IEnumerable<(SyntaxNode lambda, SyntaxNode lambdaBody1, SyntaxNode? lambdaBody2)> GetLambdaBodies(SyntaxNode body)
        {
            foreach (var node in body.DescendantNodesAndSelf())
            {
                if (TryGetLambdaBodies(node, out var body1, out var body2))
                {
                    yield return (node, body1, body2);
                }
            }
        }

        private bool CanAddNewLambda(SyntaxNode newLambda, EditAndContinueCapabilitiesGrantor capabilities, IReadOnlyDictionary<SyntaxNode, LambdaInfo>? matchedLambdas)
        {
            // New local functions mean new methods in existing classes
            if (IsLocalFunction(newLambda))
            {
                return capabilities.Grant(EditAndContinueCapabilities.AddMethodToExistingType);
            }

            // New lambdas sometimes mean creating new helper classes, and sometimes mean new methods in exising helper classes
            // Unfortunately we are limited here in what we can do here. See: https://github.com/dotnet/roslyn/issues/52759

            // If there is already a lambda in the method then the new lambda would result in a new method in the existing helper class.
            // This check is redundant with the below, once the limitation in the referenced issue is resolved
            if (matchedLambdas is { Count: > 0 })
            {
                return capabilities.Grant(EditAndContinueCapabilities.AddMethodToExistingType);
            }

            // If there is already a lambda in the class then the new lambda would result in a new method in the existing helper class.
            // If there isn't already a lambda in the class then the new lambda would result in a new helper class.
            // Unfortunately right now we can't determine which of these is true so we have to just check both capabilities instead.
            return capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition) &&
                capabilities.Grant(EditAndContinueCapabilities.AddMethodToExistingType);
        }

        private void ReportMultiScopeCaptures(
            SyntaxNode lambdaBody,
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
                                errorSpan = GetDiagnosticSpan(GetLambda(lambdaBody), EditKind.Insert);
                            }
                            else
                            {
                                errorSpan = GetVariableUseSites(GetLambdaBodyExpressionsAndStatements(lambdaBody), captures[i], model, cancellationToken).First().Span;
                            }

                            rudeEdit = RudeEditKind.InsertLambdaWithMultiScopeCapture;
                        }
                        else
                        {
                            errorSpan = newCaptures[reverseCapturesMap.IndexOf(i)].Locations.Single().SourceSpan;
                            rudeEdit = RudeEditKind.DeleteLambdaWithMultiScopeCapture;
                        }

                        diagnostics.Add(new RudeEditDiagnostic(
                            rudeEdit,
                            errorSpan,
                            null,
                            new[] { GetDisplayName(GetLambda(lambdaBody)), captures[firstAccessedCaptureIndex].Name, captures[i].Name }));

                        break;
                    }
                }
            }
        }

        private BitVector GetAccessedCaptures(SyntaxNode lambdaBody, SemanticModel model, ImmutableArray<ISymbol> captures, PooledDictionary<ISymbol, int> capturesIndex)
        {
            var result = BitVector.Create(captures.Length);

            foreach (var expressionOrStatement in GetLambdaBodyExpressionsAndStatements(lambdaBody))
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
        /// Returns node that represents a declaration of the symbol whose <paramref name="reference"/> is passed in.
        /// </summary>
        protected abstract SyntaxNode GetSymbolDeclarationSyntax(SyntaxReference reference, CancellationToken cancellationToken);

        private static TextSpan GetThisParameterDiagnosticSpan(ISymbol member)
            => member.Locations.First().SourceSpan;

        private static TextSpan GetVariableDiagnosticSpan(ISymbol local)
        {
            // Note that in VB implicit value parameter in property setter doesn't have a location.
            // In C# its location is the location of the setter.
            // See https://github.com/dotnet/roslyn/issues/14273
            return local.Locations.FirstOrDefault()?.SourceSpan ?? local.ContainingSymbol.Locations.First().SourceSpan;
        }

        private static (SyntaxNode? Node, int Ordinal) GetParameterKey(IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            var containingLambda = parameter.ContainingSymbol as IMethodSymbol;
            if (containingLambda?.MethodKind is MethodKind.LambdaMethod or MethodKind.LocalFunction)
            {
                var oldContainingLambdaSyntax = containingLambda.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);
                return (oldContainingLambdaSyntax, parameter.Ordinal);
            }
            else
            {
                return (Node: null, parameter.Ordinal);
            }
        }

        private static bool TryMapParameter((SyntaxNode? Node, int Ordinal) parameterKey, IReadOnlyDictionary<SyntaxNode, SyntaxNode> map, out (SyntaxNode? Node, int Ordinal) mappedParameterKey)
        {
            var containingLambdaSyntax = parameterKey.Node;

            if (containingLambdaSyntax == null)
            {
                // method parameter: no syntax, same ordinal (can't change since method signatures must match)
                mappedParameterKey = parameterKey;
                return true;
            }

            if (map.TryGetValue(containingLambdaSyntax, out var mappedContainingLambdaSyntax))
            {
                // parameter of an existing lambda: same ordinal (can't change since lambda signatures must match), 
                mappedParameterKey = (mappedContainingLambdaSyntax, parameterKey.Ordinal);
                return true;
            }

            // no mapping
            mappedParameterKey = default;
            return false;
        }

        private void CalculateCapturedVariablesMaps(
            ImmutableArray<ISymbol> oldCaptures,
            SyntaxNode oldMemberBody,
            ImmutableArray<ISymbol> newCaptures,
            ISymbol newMember,
            SyntaxNode newMemberBody,
            BidirectionalMap<SyntaxNode> map,
            [Out] ArrayBuilder<int> reverseCapturesMap,                  // {new capture index -> old capture index}
            [Out] ArrayBuilder<SyntaxNode?> newCapturesToClosureScopes,  // {new capture index -> new closure scope}
            [Out] ArrayBuilder<SyntaxNode?> oldCapturesToClosureScopes,  // {old capture index -> old closure scope}
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool hasErrors,
            CancellationToken cancellationToken)
        {
            hasErrors = false;

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

            using var _1 = PooledDictionary<SyntaxNode, int>.GetInstance(out var oldLocalCapturesBySyntax);
            using var _2 = PooledDictionary<(SyntaxNode? Node, int Ordinal), int>.GetInstance(out var oldParameterCapturesByLambdaAndOrdinal);

            for (var i = 0; i < oldCaptures.Length; i++)
            {
                var oldCapture = oldCaptures[i];

                if (oldCapture.Kind == SymbolKind.Parameter)
                {
                    oldParameterCapturesByLambdaAndOrdinal.Add(GetParameterKey((IParameterSymbol)oldCapture, cancellationToken), i);
                }
                else
                {
                    oldLocalCapturesBySyntax.Add(oldCapture.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken), i);
                }
            }

            for (var newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
            {
                var newCapture = newCaptures[newCaptureIndex];
                int oldCaptureIndex;

                if (newCapture.Kind == SymbolKind.Parameter)
                {
                    var newParameterCapture = (IParameterSymbol)newCapture;
                    var newParameterKey = GetParameterKey(newParameterCapture, cancellationToken);
                    if (!TryMapParameter(newParameterKey, map.Reverse, out var oldParameterKey) ||
                        !oldParameterCapturesByLambdaAndOrdinal.TryGetValue(oldParameterKey, out oldCaptureIndex))
                    {
                        // parameter has not been captured prior the edit:
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            GetVariableDiagnosticSpan(newCapture),
                            null,
                            new[] { newCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old parameter capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldParameterCapturesByLambdaAndOrdinal.Remove(oldParameterKey);
                }
                else
                {
                    var newCaptureSyntax = newCapture.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);

                    // variable doesn't exists in the old method or has not been captured prior the edit:
                    if (!map.Reverse.TryGetValue(newCaptureSyntax, out var mappedOldSyntax) ||
                        !oldLocalCapturesBySyntax.TryGetValue(mappedOldSyntax, out oldCaptureIndex))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            newCapture.Locations.First().SourceSpan,
                            null,
                            new[] { newCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldLocalCapturesBySyntax.Remove(mappedOldSyntax);
                }

                reverseCapturesMap[newCaptureIndex] = oldCaptureIndex;

                // the type and scope of parameters can't change
                if (newCapture.Kind == SymbolKind.Parameter)
                {
                    continue;
                }

                var oldCapture = oldCaptures[oldCaptureIndex];

                // Parameter capture can't be changed to local capture and vice versa
                // because parameters can't be introduced or deleted during EnC 
                // (we checked above for changes in lambda signatures).
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
                        newCapture.Locations.First().SourceSpan,
                        null,
                        new[] { oldCapture.Name, newCapture.Name }));

                    hasErrors = true;
                    continue;
                }

                // type check
                var oldTypeOpt = GetType(oldCapture);
                var newTypeOpt = GetType(newCapture);

                if (!TypesEquivalent(oldTypeOpt, newTypeOpt, exact: false))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingCapturedVariableType,
                        GetVariableDiagnosticSpan(newCapture),
                        null,
                        new[] { newCapture.Name, oldTypeOpt.ToDisplayString(ErrorDisplayFormat) }));

                    hasErrors = true;
                    continue;
                }

                // scope check:
                var oldScopeOpt = GetCapturedVariableScope(oldCapture, oldMemberBody, cancellationToken);
                var newScopeOpt = GetCapturedVariableScope(newCapture, newMemberBody, cancellationToken);
                if (!AreEquivalentClosureScopes(oldScopeOpt, newScopeOpt, map.Reverse))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingCapturedVariableScope,
                        GetVariableDiagnosticSpan(newCapture),
                        null,
                        new[] { newCapture.Name }));

                    hasErrors = true;
                    continue;
                }

                newCapturesToClosureScopes[newCaptureIndex] = newScopeOpt;
                oldCapturesToClosureScopes[oldCaptureIndex] = oldScopeOpt;
            }

            // What's left in oldCapturesBySyntax are captured variables in the previous version
            // that have no corresponding captured variables in the new version. 
            // Report a rude edit for all such variables.

            if (oldParameterCapturesByLambdaAndOrdinal.Count > 0)
            {
                // syntax-less parameters are not included:
                var newMemberParametersWithSyntax = newMember.GetParameters();

                // uncaptured parameters:
                foreach (var ((oldContainingLambdaSyntax, ordinal), oldCaptureIndex) in oldParameterCapturesByLambdaAndOrdinal)
                {
                    var oldCapture = oldCaptures[oldCaptureIndex];

                    TextSpan span;
                    if (ordinal < 0)
                    {
                        // this parameter:
                        span = GetThisParameterDiagnosticSpan(newMember);
                    }
                    else if (oldContainingLambdaSyntax != null)
                    {
                        // lambda:
                        span = GetLambdaParameterDiagnosticSpan(oldContainingLambdaSyntax, ordinal);
                    }
                    else if (oldCapture.IsImplicitValueParameter())
                    {
                        // value parameter of a property/indexer setter, event adder/remover:
                        span = newMember.Locations.First().SourceSpan;
                    }
                    else
                    {
                        // method or property:
                        span = GetVariableDiagnosticSpan(newMemberParametersWithSyntax[ordinal]);
                    }

                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.NotCapturingVariable,
                        span,
                        null,
                        new[] { oldCapture.Name }));
                }

                hasErrors = true;
            }

            if (oldLocalCapturesBySyntax.Count > 0)
            {
                // uncaptured or deleted variables:
                foreach (var entry in oldLocalCapturesBySyntax)
                {
                    var oldCaptureNode = entry.Key;
                    var oldCaptureIndex = entry.Value;
                    var name = oldCaptures[oldCaptureIndex].Name;
                    if (map.Forward.TryGetValue(oldCaptureNode, out var newCaptureNode))
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
                            GetDeletedNodeDiagnosticSpan(map.Forward, oldCaptureNode),
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
            SyntaxNode oldLambdaBody,
            SemanticModel newModel,
            SyntaxNode newLambdaBody,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasSignatureErrors,
            CancellationToken cancellationToken)
        {
            hasSignatureErrors = false;

            var newLambda = GetLambda(newLambdaBody);
            var oldLambda = GetLambda(oldLambdaBody);

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

            ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol, newLambdaSymbol, newLambda, newModel.Compilation, capabilities, out _, out _, cancellationToken);

            for (var i = 0; i < oldLambdaSymbol.Parameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol.Parameters[i], newLambdaSymbol.Parameters[i], newLambda, newModel.Compilation, capabilities, out _, out _, cancellationToken);
            }

            for (var i = 0; i < oldLambdaSymbol.TypeParameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol.TypeParameters[i], newLambdaSymbol.TypeParameters[i], newLambda, newModel.Compilation, capabilities, out _, out _, cancellationToken);
            }
        }

        private static ITypeSymbol GetType(ISymbol localOrParameter)
            => localOrParameter.Kind switch
            {
                SymbolKind.Parameter => ((IParameterSymbol)localOrParameter).Type,
                SymbolKind.Local => ((ILocalSymbol)localOrParameter).Type,
                _ => throw ExceptionUtilities.UnexpectedValue(localOrParameter.Kind),
            };

        private SyntaxNode GetCapturedVariableScope(ISymbol localOrParameter, SyntaxNode memberBody, CancellationToken cancellationToken)
        {
            Debug.Assert(localOrParameter.Kind != SymbolKind.RangeVariable);

            if (localOrParameter.Kind == SymbolKind.Parameter)
            {
                var member = localOrParameter.ContainingSymbol;

                // lambda parameters and C# constructor parameters are lifted to their own scope:
                if ((member as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction || HasParameterClosureScope(member))
                {
                    var result = localOrParameter.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);
                    Debug.Assert(IsLambda(result));
                    return result;
                }

                return memberBody;
            }

            var node = localOrParameter.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);
            while (true)
            {
                RoslynDebug.Assert(node is object);
                if (IsClosureScope(node))
                {
                    return node;
                }

                node = node.Parent;
            }
        }

        private static bool AreEquivalentClosureScopes(SyntaxNode oldScopeOpt, SyntaxNode newScopeOpt, IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
        {
            if (oldScopeOpt == null || newScopeOpt == null)
            {
                return oldScopeOpt == newScopeOpt;
            }

            return reverseMap.TryGetValue(newScopeOpt, out var mappedScope) && mappedScope == oldScopeOpt;
        }

        #endregion

        #region State Machines

        private void ReportStateMachineRudeEdits(
            Compilation oldCompilation,
            ISymbol oldMember,
            SyntaxNode newBody,
            ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            // Only methods, local functions and anonymous functions can be async/iterators machines, 
            // but don't assume so to be resiliant against errors in code.
            if (oldMember is not IMethodSymbol oldMethod)
            {
                return;
            }

            var stateMachineAttributeQualifiedName = oldMethod.IsAsync ?
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" :
                "System.Runtime.CompilerServices.IteratorStateMachineAttribute";

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
                    GetBodyDiagnosticSpan(newBody, EditKind.Update),
                    newBody,
                    new[] { stateMachineAttributeQualifiedName }));
            }
        }

        #endregion

        #endregion

        #region Helpers

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

        private static bool InGenericContext(ISymbol symbol, out bool isGenericMethod)
        {
            var current = symbol;

            while (true)
            {
                if (current is IMethodSymbol { Arity: > 0 })
                {
                    isGenericMethod = true;
                    return true;
                }

                if (current is INamedTypeSymbol { Arity: > 0 })
                {
                    isGenericMethod = false;
                    return true;
                }

                current = current.ContainingSymbol;
                if (current == null)
                {
                    isGenericMethod = false;
                    return false;
                }
            }
        }

        #endregion

        #region Testing

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly AbstractEditAndContinueAnalyzer _abstractEditAndContinueAnalyzer;

            public TestAccessor(AbstractEditAndContinueAnalyzer abstractEditAndContinueAnalyzer)
                => _abstractEditAndContinueAnalyzer = abstractEditAndContinueAnalyzer;

            internal void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, EditScript<SyntaxNode> syntacticEdits, Dictionary<SyntaxNode, EditKind> editMap)
                => _abstractEditAndContinueAnalyzer.ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits, editMap);

            internal BidirectionalMap<SyntaxNode> ComputeMap(
                Match<SyntaxNode> bodyMatch,
                ArrayBuilder<ActiveNode> activeNodes,
                ref Dictionary<SyntaxNode, LambdaInfo>? lazyActiveOrMatchedLambdas,
                ArrayBuilder<RudeEditDiagnostic> diagnostics)
            {
                return _abstractEditAndContinueAnalyzer.ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics);
            }

            internal Match<SyntaxNode> ComputeBodyMatch(
                SyntaxNode oldBody,
                SyntaxNode newBody,
                ActiveNode[] activeNodes,
                ArrayBuilder<RudeEditDiagnostic> diagnostics,
                out bool oldHasStateMachineSuspensionPoint,
                out bool newHasStateMachineSuspensionPoint)
            {
                return _abstractEditAndContinueAnalyzer.ComputeBodyMatch(oldBody, newBody, activeNodes, diagnostics, out oldHasStateMachineSuspensionPoint, out newHasStateMachineSuspensionPoint);
            }
        }

        #endregion
    }
}
