// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
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
        internal abstract bool ExperimentalFeaturesEnabled(SyntaxTree tree);

        /// <summary>
        /// Finds a member declaration node containing given active statement node.
        /// </summary>
        /// <remarks>
        /// The implementation has to decide what kinds of nodes in top-level match relationship represent a declaration.
        /// Every member declaration must be represented by exactly one node, but not all nodes have to represent a declaration.
        /// </remarks>
        internal abstract SyntaxNode FindMemberDeclaration(SyntaxNode root, SyntaxNode node);

        internal SyntaxNode FindMemberDeclaration(SyntaxNode root, int activeStatementStart)
        {
            var node = TryGetNode(root, activeStatementStart);
            return (node != null) ? FindMemberDeclaration(root, node) : null;
        }

        /// <summary>
        /// If the specified node represents a member declaration returns a node that represents its body,
        /// i.e. a node used as the root of statement-level match.
        /// </summary>
        /// <param name="node">A node representing a declaration or a top-level edit node.</param>
        /// <param name="isMember">
        /// True if <paramref name="isMember"/> represents a member declaration,
        /// false if it represents an edit node.</param>
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
        ///      Body if a VB field declaration with shared AsNew initializer is the New expression. Active statements might be placed on the field variables.
        /// <see cref="FindStatementAndPartner"/> has to account for such cases.
        /// </remarks>
        internal abstract SyntaxNode TryGetDeclarationBody(SyntaxNode node, bool isMember);

        /// <summary>
        /// Interprets an edit as a declaration body edit.
        /// </summary>
        /// <param name="edit">A top-level edit.</param>
        /// <param name="editMap">All top-level edits by syntax node.</param>
        /// <param name="oldBody">The old body participating in the edit.</param>
        /// <param name="newBody">The new body participating in the edit.</param>
        /// <returns>
        /// True if the specified edit is a declaration body edit, false otherwise.
        /// </returns>
        protected virtual bool TryGetDeclarationBodyEdit(
            Edit<SyntaxNode> edit,
            Dictionary<SyntaxNode, EditKind> editMap,
            out SyntaxNode oldBody,
            out SyntaxNode newBody)
        {
            oldBody = (edit.OldNode != null) ? TryGetDeclarationBody(edit.OldNode, isMember: false) : null;
            newBody = (edit.NewNode != null) ? TryGetDeclarationBody(edit.NewNode, isMember: false) : null;
            return true;
        }

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
        /// </remarks>
        internal abstract IEnumerable<SyntaxToken> TryGetActiveTokens(SyntaxNode node);

        /// <summary>
        /// Returns an ancestor that encompasses all active and statement level 
        /// nodes that belong to the member represented by <paramref name="bodyOrMatchRootOpt"/>.
        /// </summary>
        protected SyntaxNode GetEncompassingAncestor(SyntaxNode bodyOrMatchRootOpt)
        {
            if (bodyOrMatchRootOpt == null)
            {
                return null;
            }

            var root = GetEncompassingAncestorImpl(bodyOrMatchRootOpt);
            Debug.Assert(root.Span.Contains(bodyOrMatchRootOpt.Span));
            return root;
        }

        protected abstract SyntaxNode GetEncompassingAncestorImpl(SyntaxNode bodyOrMatchRoot);

        /// <summary>
        /// Finds a statement at given span and a declaration body.
        /// Also returns the corresponding partner statement in <paramref name="partnerDeclarationBodyOpt"/>, if specified.
        /// </summary>
        /// <remarks>
        /// The declaration body node may not contain the <paramref name="span"/>. 
        /// This happens when an active statement associated with the member is outside of its body (e.g. C# constructor).
        /// If the position doesn't correspond to any statement uses the start of the <paramref name="declarationBody"/>.
        /// </remarks>
        protected abstract SyntaxNode FindStatementAndPartner(SyntaxNode declarationBody, TextSpan span, SyntaxNode partnerDeclarationBodyOpt, out SyntaxNode partnerOpt, out int statementPart);

        private SyntaxNode FindStatement(SyntaxNode declarationBody, TextSpan span, out int statementPart)
            => FindStatementAndPartner(declarationBody, span, null, out _, out statementPart);

        /// <summary>
        /// Maps <paramref name="leftNode"/> descendant of <paramref name="leftRoot"/> to corresponding descendant node
        /// of <paramref name="rightRoot"/>, assuming that the trees only differ in trivia
        /// </summary>
        internal abstract SyntaxNode FindPartner(SyntaxNode leftRoot, SyntaxNode rightRoot, SyntaxNode leftNode);

        internal abstract SyntaxNode FindPartnerInMemberInitializer(SemanticModel leftModel, INamedTypeSymbol leftType, SyntaxNode leftNode, INamedTypeSymbol rightType, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a node that represents a body of a lambda containing specified <paramref name="node"/>,
        /// or null if the node isn't contained in a lambda. If a node is returned it must uniquely represent the lambda,
        /// i.e. be no two distinct nodes may represent the same lambda.
        /// </summary>
        protected abstract SyntaxNode FindEnclosingLambdaBody(SyntaxNode containerOpt, SyntaxNode node);

        /// <summary>
        /// Given a node that represents a lambda body returns all nodes of the body in a syntax list.
        /// </summary>
        /// <remarks>
        /// Note that VB lambda bodies are represented by a lambda header and that some lambda bodies share 
        /// their parent nodes with other bodies (e.g. join clause expressions).
        /// </remarks>
        protected abstract IEnumerable<SyntaxNode> GetLambdaBodyExpressionsAndStatements(SyntaxNode lambdaBody);

        protected abstract SyntaxNode TryGetPartnerLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda);

        protected abstract Match<SyntaxNode> ComputeTopLevelMatch(SyntaxNode oldCompilationUnit, SyntaxNode newCompilationUnit);
        protected abstract Match<SyntaxNode> ComputeBodyMatch(SyntaxNode oldBody, SyntaxNode newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> knownMatches);
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
            out SyntaxNode newStatement);

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
        protected abstract IEnumerable<KeyValuePair<SyntaxNode, int>> EnumerateNearStatements(SyntaxNode statement);

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

        protected abstract ISymbol GetSymbolForEdit(SemanticModel model, SyntaxNode node, EditKind editKind, Dictionary<SyntaxNode, EditKind> editMap, CancellationToken cancellationToken);

        /// <summary>
        /// Analyzes data flow in the member body represented by the specified node and returns all captured variables and parameters (including "this").
        /// If the body is a field/property initializer analyzes the initializer expression only.
        /// </summary>
        protected abstract ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model, SyntaxNode memberBody);

        /// <summary>
        /// Enumerates all use sites of a specified variable within the specified syntax subtrees.
        /// </summary>
        protected abstract IEnumerable<SyntaxNode> GetVariableUseSites(IEnumerable<SyntaxNode> roots, ISymbol localOrParameter, SemanticModel model, CancellationToken cancellationToken);

        // diagnostic spans:
        protected abstract TextSpan? TryGetDiagnosticSpan(SyntaxNode node, EditKind editKind);

        internal TextSpan GetDiagnosticSpan(SyntaxNode node, EditKind editKind)
          => TryGetDiagnosticSpan(node, editKind) ?? node.Span;

        protected virtual TextSpan GetBodyDiagnosticSpan(SyntaxNode node, EditKind editKind)
        {
            var initialNode = node;

            while (true)
            {
                node = node.Parent;
                if (node == null)
                {
                    return initialNode.Span;
                }

                var span = TryGetDiagnosticSpan(node, editKind);
                if (span != null)
                {
                    return span.Value;
                }
            }
        }

        internal abstract TextSpan GetLambdaParameterDiagnosticSpan(SyntaxNode lambda, int ordinal);

        // display names:
        internal string GetDisplayName(SyntaxNode node, EditKind editKind = EditKind.Update)
          => TryGetDisplayName(node, editKind) ?? throw ExceptionUtilities.UnexpectedValue(node.GetType().Name);

        /// <summary>
        /// Returns the display name of an ancestor node that contains the specified node and has a display name.
        /// </summary>
        protected virtual string GetBodyDisplayName(SyntaxNode node, EditKind editKind = EditKind.Update)
        {
            var initialNode = node;
            while (true)
            {
                node = node.Parent;
                if (node == null)
                {
                    throw ExceptionUtilities.UnexpectedValue(initialNode.GetType().Name);
                }

                var displayName = TryGetDisplayName(node, editKind);
                if (displayName != null)
                {
                    return displayName;
                }
            }
        }

        protected abstract string TryGetDisplayName(SyntaxNode node, EditKind editKind);

        protected virtual string GetSuspensionPointDisplayName(SyntaxNode node, EditKind editKind)
            => GetDisplayName(node, editKind);

        protected abstract SymbolDisplayFormat ErrorDisplayFormat { get; }
        protected abstract List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, bool isNonLeaf);
        protected abstract void GetStateMachineInfo(SyntaxNode body, out ImmutableArray<SyntaxNode> suspensionPoints, out StateMachineKinds kinds);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportSyntacticRudeEdits(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(List<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, TextSpan newStatementSpan);
        internal abstract void ReportOtherRudeEditsAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode oldStatement, SyntaxNode newStatement, bool isNonLeaf);
        internal abstract void ReportMemberUpdateRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode newMember, TextSpan? span);
        internal abstract void ReportInsertedMemberSymbolRudeEdits(List<RudeEditDiagnostic> diagnostics, ISymbol newSymbol);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract bool IsLambda(SyntaxNode node);
        internal abstract bool IsInterfaceDeclaration(SyntaxNode node);

        /// <summary>
        /// True if the node represents any form of a function definition nested in another function body (i.e. anonymous function, lambda, local function).
        /// </summary>
        internal abstract bool IsNestedFunction(SyntaxNode node);

        internal abstract bool IsLocalFunction(SyntaxNode node);
        internal abstract bool IsClosureScope(SyntaxNode node);
        internal abstract bool ContainsLambda(SyntaxNode declaration);
        internal abstract SyntaxNode GetLambda(SyntaxNode lambdaBody);
        internal abstract IMethodSymbol GetLambdaExpressionSymbol(SemanticModel model, SyntaxNode lambdaExpression, CancellationToken cancellationToken);
        internal abstract SyntaxNode GetContainingQueryExpression(SyntaxNode node);
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
        internal abstract bool TryGetLambdaBodies(SyntaxNode node, out SyntaxNode body1, out SyntaxNode body2);

        internal abstract bool IsStateMachineMethod(SyntaxNode declaration);
        internal abstract SyntaxNode TryGetContainingTypeDeclaration(SyntaxNode node);

        internal abstract bool HasBackingField(SyntaxNode propertyDeclaration);

        /// <summary>
        /// Return true if the declaration is a field/property declaration with an initializer. 
        /// Shall return false for enum members.
        /// </summary>
        internal abstract bool IsDeclarationWithInitializer(SyntaxNode declaration);

        /// <summary>
        /// Return true if the declaration is a constructor declaration to which field/property initializers are emitted. 
        /// </summary>
        internal abstract bool IsConstructorWithMemberInitializers(SyntaxNode declaration);

        internal abstract bool IsPartial(INamedTypeSymbol type);
        internal abstract SyntaxNode EmptyCompilationUnit { get; }

        private static readonly SourceText s_emptySource = SourceText.From("");

        #region Document Analysis 

        public async Task<DocumentAnalysisResults> AnalyzeDocumentAsync(
            Project baseProjectOpt,
            ImmutableArray<ActiveStatement> baseActiveStatements,
            Document document,
            IActiveStatementTrackingService trackingServiceOpt,
            CancellationToken cancellationToken)
        {
            DocumentAnalysisResults.Log.Write("Analyzing document {0}", document.Name);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxTree oldTreeOpt;
                SyntaxNode oldRoot;
                SourceText oldText;

                var oldDocumentOpt = baseProjectOpt?.GetDocument(document.Id);
                if (oldDocumentOpt != null)
                {
                    oldTreeOpt = await oldDocumentOpt.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    oldRoot = await oldTreeOpt.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    oldText = await oldDocumentOpt.GetTextAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    oldTreeOpt = null;
                    oldRoot = EmptyCompilationUnit;
                    oldText = s_emptySource;
                }

                var newTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // Changes in parse options might change the meaning of the code even if nothing else changed.
                // The IDE should disallow changing the options during debugging session. 
                Debug.Assert(oldTreeOpt == null || oldTreeOpt.Options.Equals(newTree.Options));

                var newRoot = await newTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                // TODO: newTree.HasErrors?
                var syntaxDiagnostics = newRoot.GetDiagnostics();
                var syntaxErrorCount = syntaxDiagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

                var newActiveStatements = new ActiveStatement[baseActiveStatements.Length];
                var newExceptionRegions = (syntaxErrorCount == 0) ? new ImmutableArray<LinePositionSpan>[baseActiveStatements.Length] : null;

                if (oldText.ContentEquals(newText))
                {
                    // The document might have been closed and reopened, which might have triggered analysis. 
                    // If the document is unchanged don't continue the analysis since 
                    // a) comparing texts is cheaper than diffing trees
                    // b) we need to ignore errors in unchanged documents

                    AnalyzeUnchangedDocument(
                        baseActiveStatements,
                        newText,
                        newRoot,
                        document.Id,
                        trackingServiceOpt,
                        newActiveStatements,
                        newExceptionRegions);

                    if (syntaxErrorCount > 0)
                    {
                        DocumentAnalysisResults.Log.Write("{0}: unchanged with syntax errors", document.Name);
                    }
                    else
                    {
                        DocumentAnalysisResults.Log.Write("{0}: unchanged", document.Name);
                    }

                    return DocumentAnalysisResults.Unchanged(newActiveStatements.AsImmutable(), newExceptionRegions.AsImmutableOrNull());
                }

                if (syntaxErrorCount > 0)
                {
                    // Bail, since we can't do syntax diffing on broken trees (it would not produce useful results anyways).
                    // If we needed to do so for some reason, we'd need to harden the syntax tree comparers.
                    DocumentAnalysisResults.Log.Write("Syntax errors: {0} total", syntaxErrorCount);

                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray<RudeEditDiagnostic>.Empty);
                }

                // Disallow modification of a file with experimental features enabled.
                // These features may not be handled well by the analysis below.
                if (ExperimentalFeaturesEnabled(newTree))
                {
                    DocumentAnalysisResults.Log.Write("{0}: experimental features enabled", document.Name);

                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.ExperimentalFeaturesEnabled, default)));
                }

                // We do calculate diffs even if there are semantic errors for the following reasons: 
                // 1) We need to be able to find active spans in the new document. 
                //    If we didn't calculate them we would only rely on tracking spans (might be ok).
                // 2) If there are syntactic rude edits we'll report them faster without waiting for semantic analysis.
                //    The user may fix them before they address all the semantic errors.

                var updatedMethods = new List<UpdatedMemberInfo>();
                var diagnostics = new List<RudeEditDiagnostic>();

                cancellationToken.ThrowIfCancellationRequested();

                var topMatch = ComputeTopLevelMatch(oldRoot, newRoot);
                var syntacticEdits = topMatch.GetTreeEdits();
                var editMap = BuildEditMap(syntacticEdits);

                AnalyzeSyntax(
                    syntacticEdits,
                    editMap,
                    oldText,
                    newText,
                    document.Id,
                    trackingServiceOpt,
                    baseActiveStatements,
                    newActiveStatements,
                    newExceptionRegions,
                    updatedMethods,
                    diagnostics);

                if (diagnostics.Count > 0)
                {
                    DocumentAnalysisResults.Log.Write("{0} syntactic rude edits, first: '{1}'", diagnostics.Count, document.FilePath);
                    return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), diagnostics.AsImmutable());
                }

                // Disallow addition of a new file.
                // During EnC, a new file cannot be added to the current solution, but some IDE features (i.e., CodeFix) try to do so. 
                // In most cases, syntactic rude edits detect them with specific reasons but some reach up to here and we bail them out with a general message.
                if (oldDocumentOpt == null)
                {
                    DocumentAnalysisResults.Log.Write("A new file added: {0}", document.Name);
                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.InsertFile, default)));
                }

                cancellationToken.ThrowIfCancellationRequested();

                var triviaEdits = new List<(SyntaxNode OldNode, SyntaxNode NewNode)>();
                var lineEdits = new List<LineChange>();

                AnalyzeTrivia(
                    oldText,
                    newText,
                    topMatch,
                    editMap,
                    triviaEdits,
                    lineEdits,
                    diagnostics,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (diagnostics.Count > 0)
                {
                    DocumentAnalysisResults.Log.Write("{0} trivia rude edits, first: {1}@{2}", diagnostics.Count, document.FilePath, diagnostics.First().Span.Start);
                    return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), diagnostics.AsImmutable());
                }

                cancellationToken.ThrowIfCancellationRequested();

                List<SemanticEdit> semanticEdits = null;
                if (syntacticEdits.Edits.Length > 0 || triviaEdits.Count > 0)
                {
                    var newModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var oldModel = await oldDocumentOpt.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    semanticEdits = new List<SemanticEdit>();
                    AnalyzeSemantics(
                        syntacticEdits,
                        editMap,
                        oldText,
                        baseActiveStatements,
                        triviaEdits,
                        updatedMethods,
                        oldModel,
                        newModel,
                        semanticEdits,
                        diagnostics,
                        out var firstDeclaratingErrorOpt,
                        cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (firstDeclaratingErrorOpt != null)
                    {
                        var location = firstDeclaratingErrorOpt.Location;
                        DocumentAnalysisResults.Log.Write("Declaration errors, first: {0}", location.IsInSource ? location.SourceTree.FilePath : location.MetadataModule.Name);

                        return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), ImmutableArray.Create<RudeEditDiagnostic>(), hasSemanticErrors: true);
                    }

                    if (diagnostics.Count > 0)
                    {
                        DocumentAnalysisResults.Log.Write("{0}@{1}: semantic rude edit ({2} total)", document.FilePath, diagnostics.First().Span.Start, diagnostics.Count);
                        return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), diagnostics.AsImmutable());
                    }
                }

                return new DocumentAnalysisResults(
                    newActiveStatements.AsImmutable(),
                    diagnostics.AsImmutable(),
                    semanticEdits.AsImmutableOrEmpty(),
                    newExceptionRegions.AsImmutable(),
                    lineEdits.AsImmutable(),
                    hasSemanticErrors: false);
            }
            catch (Exception e) when (ReportFatalErrorAnalyzeDocumentAsync(baseActiveStatements, e))
            {
                // The same behavior as if there was a syntax error - we are unable to analyze the document. 
                return DocumentAnalysisResults.SyntaxErrors(ImmutableArray.Create(
                    new RudeEditDiagnostic(RudeEditKind.InternalError, span: default, arguments: new[] { document.FilePath, e.ToString() })));
            }
        }

        // Active statements spans are usually unavailable in crash dumps due to a bug in the debugger (DevDiv #150901), 
        // so we stash them here in plain array (can't use immutable, see the bug) just before we report NFW.
        private static ActiveStatement[] s_fatalErrorBaseActiveStatements;

        private static bool ReportFatalErrorAnalyzeDocumentAsync(ImmutableArray<ActiveStatement> baseActiveStatements, Exception e)
        {
            if (!(e is OperationCanceledException))
            {
                s_fatalErrorBaseActiveStatements = baseActiveStatements.ToArray();
            }

            return FatalError.ReportWithoutCrashUnlessCanceled(e);
        }

        internal Dictionary<SyntaxNode, EditKind> BuildEditMap(EditScript<SyntaxNode> editScript)
        {
            var map = new Dictionary<SyntaxNode, EditKind>(editScript.Edits.Length);

            foreach (var edit in editScript.Edits)
            {
                // do not include reorder and move edits

                if (edit.Kind == EditKind.Delete || edit.Kind == EditKind.Update)
                {
                    map.Add(edit.OldNode, edit.Kind);
                }

                if (edit.Kind == EditKind.Insert || edit.Kind == EditKind.Update)
                {
                    map.Add(edit.NewNode, edit.Kind);
                }
            }

            return map;
        }

        #endregion

        #region Syntax Analysis 

        private void AnalyzeSyntax(
            EditScript<SyntaxNode> script,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingServiceOpt,
            ImmutableArray<ActiveStatement> oldActiveStatements,
            [Out]ActiveStatement[] newActiveStatements,
            [Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [Out]List<UpdatedMemberInfo> updatedMethods,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);
            Debug.Assert(updatedMethods != null);
            Debug.Assert(updatedMethods.Count == 0);

            var updatedTrackingSpans = ArrayBuilder<(ActiveStatementId, ActiveStatementTextSpan)>.GetInstance();

            for (var i = 0; i < script.Edits.Length; i++)
            {
                var edit = script.Edits[i];

                AnalyzeUpdatedActiveMethodBodies(script, i, editMap, oldText, newText, documentId, trackingServiceOpt, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedMethods, updatedTrackingSpans, diagnostics);
                ReportSyntacticRudeEdits(diagnostics, script.Match, edit, editMap);
            }

            UpdateUneditedSpans(diagnostics, script.Match, oldText, newText, documentId, trackingServiceOpt, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedTrackingSpans);

            Debug.Assert(newActiveStatements.All(a => a != null));

            if (updatedTrackingSpans.Count > 0)
            {
                trackingServiceOpt.UpdateActiveStatementSpans(newText, updatedTrackingSpans);
            }

            updatedTrackingSpans.Free();
        }

        private void UpdateUneditedSpans(
            List<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> topMatch,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingServiceOpt,
            ImmutableArray<ActiveStatement> oldActiveStatements,
            [In, Out]ActiveStatement[] newActiveStatements,
            [In, Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [In, Out]ArrayBuilder<(ActiveStatementId, ActiveStatementTextSpan)> updatedTrackingSpans)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);

            // Active statements in methods that were not updated 
            // are not changed but their spans might have been. 

            for (var i = 0; i < newActiveStatements.Length; i++)
            {
                if (newActiveStatements[i] == null)
                {
                    Contract.ThrowIfFalse(newExceptionRegions[i].IsDefault);
                    TextSpan trackedSpan = default;
                    var isTracked = trackingServiceOpt != null &&
                                     trackingServiceOpt.TryGetSpan(new ActiveStatementId(documentId, i), newText, out trackedSpan);
                    if (!TryGetTextSpan(oldText.Lines, oldActiveStatements[i].Span, out var oldStatementSpan))
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
                        newActiveStatements[i] = oldActiveStatements[i].WithSpan(default);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    var oldMember = FindMemberDeclaration(topMatch.OldRoot, oldStatementSpan.Start);

                    // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                    if (oldMember == null)
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
                        newActiveStatements[i] = oldActiveStatements[i].WithSpan(default);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    var hasPartner = topMatch.TryGetNewNode(oldMember, out var newMember);
                    Contract.ThrowIfFalse(hasPartner);

                    var oldBody = TryGetDeclarationBody(oldMember, isMember: true);
                    var newBody = TryGetDeclarationBody(newMember, isMember: true);

                    // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                    if (oldBody == null || newBody == null)
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
                        newActiveStatements[i] = oldActiveStatements[i].WithSpan(default);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    var statementPart = -1;
                    SyntaxNode newStatement = null;

                    // The tracking span might have been deleted or moved outside of the member span.
                    // It is not an error to move the statement - we just ignore it.
                    if (isTracked && trackedSpan.Length != 0 && newMember.Span.Contains(trackedSpan))
                    {
                        var trackedStatement = FindStatement(newBody, trackedSpan, out var trackedStatementPart);
                        Contract.ThrowIfNull(trackedStatement);

                        // Adjust for active statements that cover more than the old member span.
                        // For example, C# variable declarators that represent field initializers:
                        //   [|public int <<F = Expr()>>;|]
                        var adjustedOldStatementStart = oldMember.FullSpan.Contains(oldStatementSpan.Start) ? oldStatementSpan.Start : oldMember.SpanStart;

                        // The tracking span might have been moved outside of lambda.
                        // It is not an error to move the statement - we just ignore it.
                        var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody, oldMember.FindToken(adjustedOldStatementStart).Parent);
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
                        var ancestors = GetExceptionHandlingAncestors(newStatement, oldActiveStatements[i].IsNonLeaf);
                        newExceptionRegions[i] = GetExceptionRegions(ancestors, newText);
                    }

                    // Even though the body of the declaration haven't changed, 
                    // changes to its header might have caused the active span to become unavailable.
                    // (e.g. In C# "const" was added to modifiers of a field with an initializer).
                    var newStatementSpan = FindClosestActiveSpan(newStatement, statementPart);

                    newActiveStatements[i] = oldActiveStatements[i].WithSpan(newText.Lines.GetLinePositionSpan(newStatementSpan));

                    // Update tracking span if we found a matching active statement whose span is different.
                    if (isTracked && newStatementSpan != trackedSpan)
                    {
                        updatedTrackingSpans.Add((new ActiveStatementId(documentId, i), new ActiveStatementTextSpan(oldActiveStatements[i].Flags, newStatementSpan)));
                    }
                }
            }
        }

        private void AnalyzeUnchangedDocument(
            ImmutableArray<ActiveStatement> oldActiveStatements,
            SourceText newText,
            SyntaxNode newRoot,
            DocumentId documentId,
            IActiveStatementTrackingService trackingServiceOpt,
            [In, Out]ActiveStatement[] newActiveStatements,
            [In, Out]ImmutableArray<LinePositionSpan>[] newExceptionRegionsOpt)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(newExceptionRegionsOpt == null || oldActiveStatements.Length == newExceptionRegionsOpt.Length);

            var updatedTrackingSpans = ArrayBuilder<(ActiveStatementId, ActiveStatementTextSpan)>.GetInstance();

            // Active statements in methods that were not updated 
            // are not changed but their spans might have been. 

            for (var i = 0; i < newActiveStatements.Length; i++)
            {
                if (!TryGetTextSpan(newText.Lines, oldActiveStatements[i].Span, out var oldStatementSpan) ||
                    !TryGetEnclosingBreakpointSpan(newRoot, oldStatementSpan.Start, out var newStatementSpan))
                {
                    newActiveStatements[i] = oldActiveStatements[i].WithSpan(default);
                    newExceptionRegionsOpt[i] = ImmutableArray<LinePositionSpan>.Empty;
                    continue;
                }

                var newNode = TryGetNode(newRoot, oldStatementSpan.Start);
                Debug.Assert(newNode != null); // we wouldn't find a breakpoint span otherwise

                if (newExceptionRegionsOpt != null)
                {
                    var ancestors = GetExceptionHandlingAncestors(newNode, oldActiveStatements[i].IsNonLeaf);
                    newExceptionRegionsOpt[i] = GetExceptionRegions(ancestors, newText);
                }

                newActiveStatements[i] = oldActiveStatements[i].WithSpan(newText.Lines.GetLinePositionSpan(newStatementSpan));

                // Update tracking span if we found a matching active statement whose span is different.
                TextSpan trackedSpan = default;
                var isTracked = trackingServiceOpt != null &&
                                 trackingServiceOpt.TryGetSpan(new ActiveStatementId(documentId, i), newText, out trackedSpan);

                if (isTracked && newStatementSpan != trackedSpan)
                {
                    updatedTrackingSpans.Add((new ActiveStatementId(documentId, i), new ActiveStatementTextSpan(oldActiveStatements[i].Flags, newStatementSpan)));
                }
            }

            if (updatedTrackingSpans.Count > 0)
            {
                trackingServiceOpt.UpdateActiveStatementSpans(newText, updatedTrackingSpans);
            }

            updatedTrackingSpans.Free();
        }

        internal readonly struct ActiveNode
        {
            public readonly SyntaxNode OldNode;
            public readonly SyntaxNode NewTrackedNodeOpt;
            public readonly SyntaxNode EnclosingLambdaBodyOpt;
            public readonly int StatementPart;
            public readonly TextSpan? TrackedSpanOpt;

            public ActiveNode(SyntaxNode oldNode, SyntaxNode enclosingLambdaBodyOpt, int statementPart, TextSpan? trackedSpanOpt, SyntaxNode newTrackedNodeOpt)
            {
                Debug.Assert(oldNode != null);

                OldNode = oldNode;
                NewTrackedNodeOpt = newTrackedNodeOpt;
                EnclosingLambdaBodyOpt = enclosingLambdaBodyOpt;
                StatementPart = statementPart;
                TrackedSpanOpt = trackedSpanOpt;
            }
        }

        internal readonly struct LambdaInfo
        {
            public readonly List<int> ActiveNodeIndices;
            public readonly Match<SyntaxNode> Match;
            public readonly SyntaxNode NewBody;

            public LambdaInfo(List<int> activeNodeIndices)
                : this(activeNodeIndices, null, null)
            {
            }

            private LambdaInfo(List<int> activeNodeIndices, Match<SyntaxNode> match, SyntaxNode newLambdaBody)
            {
                ActiveNodeIndices = activeNodeIndices;
                Match = match;
                NewBody = newLambdaBody;
            }

            public LambdaInfo WithMatch(Match<SyntaxNode> match, SyntaxNode newLambdaBody)
            {
                return new LambdaInfo(ActiveNodeIndices, match, newLambdaBody);
            }
        }

        internal readonly struct UpdatedMemberInfo
        {
            // Index in top edit script.
            public readonly int EditOrdinal;

            // node that represents the old body of the method:
            public readonly SyntaxNode OldBody;

            // node that represents the new body of the method:
            public readonly SyntaxNode NewBody;

            // { NewNode <-> OldNode }
            public readonly BidirectionalMap<SyntaxNode> Map;

            // { OldLambdaBody -> LambdaInfo }
            public readonly IReadOnlyDictionary<SyntaxNode, LambdaInfo> ActiveOrMatchedLambdasOpt;

            // the method has an active statement (the statement might be in the body itself or in a lambda)
            public readonly bool HasActiveStatement;

            // The old method body has a suspension point (await/yield); 
            // only true if the body itself has the suspension point, not if it contains async/iterator lambda
            public readonly bool OldHasStateMachineSuspensionPoint;

            // The new method body has a suspension point (await/yield); 
            // only true if the body itself has the suspension point, not if it contains async/iterator lambda
            public readonly bool NewHasStateMachineSuspensionPoint;

            public UpdatedMemberInfo(
                int editOrdinal,
                SyntaxNode oldBody,
                SyntaxNode newBody,
                BidirectionalMap<SyntaxNode> map,
                IReadOnlyDictionary<SyntaxNode, LambdaInfo> activeOrMatchedLambdasOpt,
                bool hasActiveStatement,
                bool oldHasStateMachineSuspensionPoint,
                bool newHasStateMachineSuspensionPoint)
            {
                Debug.Assert(editOrdinal >= 0);
                Debug.Assert(!map.IsDefaultOrEmpty);
                Debug.Assert(oldBody != null);
                Debug.Assert(newBody != null);

                EditOrdinal = editOrdinal;
                OldBody = oldBody;
                NewBody = newBody;
                Map = map;
                ActiveOrMatchedLambdasOpt = activeOrMatchedLambdasOpt;
                HasActiveStatement = hasActiveStatement;
                OldHasStateMachineSuspensionPoint = oldHasStateMachineSuspensionPoint;
                NewHasStateMachineSuspensionPoint = newHasStateMachineSuspensionPoint;
            }
        }

        private void AnalyzeUpdatedActiveMethodBodies(
            EditScript<SyntaxNode> topEditScript,
            int editOrdinal,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingServiceOpt,
            ImmutableArray<ActiveStatement> oldActiveStatements,
            [Out]ActiveStatement[] newActiveStatements,
            [Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [Out]List<UpdatedMemberInfo> updatedMembers,
            [Out]ArrayBuilder<(ActiveStatementId, ActiveStatementTextSpan)> updatedTrackingSpans,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);

            var edit = topEditScript.Edits[editOrdinal];

            // new code can't contain active statements, code that moved doesn't contain updates:
            if (edit.Kind == EditKind.Insert || edit.Kind == EditKind.Reorder || edit.Kind == EditKind.Move)
            {
                return;
            }

            if (!TryGetDeclarationBodyEdit(edit, editMap, out var oldBody, out var newBody) || oldBody == null)
            {
                return;
            }

            var hasActiveStatement = TryGetOverlappingActiveStatements(oldText, edit.OldNode.Span, oldActiveStatements, out var start, out var end);

            if (edit.Kind == EditKind.Delete)
            {
                // The entire member has been deleted.

                // TODO: if the member isn't a field/property we should return empty span.
                // We need to adjust the tracking span design and UpdateUneditedSpans to account for such empty spans.
                if (hasActiveStatement)
                {
                    var newSpan = IsDeclarationWithInitializer(edit.OldNode) ?
                        GetDeletedNodeActiveSpan(topEditScript.Match.Matches, edit.OldNode) :
                        GetDeletedNodeDiagnosticSpan(topEditScript.Match.Matches, edit.OldNode);

                    for (var i = start; i < end; i++)
                    {
                        // TODO: VB field multi-initializers break this
                        // Debug.Assert(newActiveStatements[i] == default(LinePositionSpan));

                        newActiveStatements[i] = oldActiveStatements[i].WithSpan(newText.Lines.GetLinePositionSpan(newSpan));
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                    }
                }

                return;
            }

            if (newBody == null)
            {
                // The body has been deleted.

                if (hasActiveStatement)
                {
                    var newSpan = FindClosestActiveSpan(edit.NewNode, 0);
                    for (var i = start; i < end; i++)
                    {
                        Debug.Assert(newActiveStatements[i] == null && newSpan != default);
                        newActiveStatements[i] = oldActiveStatements[i].WithSpan(newText.Lines.GetLinePositionSpan(newSpan));
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                    }
                }

                return;
            }

            // Populated with active lambdas and matched lambdas. 
            // Unmatched non-active lambdas are not included.
            // { old-lambda-body -> info }
            Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas = null;

            // finds leaf nodes that correspond to the old active statements:
            Debug.Assert(end > start || !hasActiveStatement && end == start);
            var activeNodes = new ActiveNode[end - start];
            for (var i = 0; i < activeNodes.Length; i++)
            {
                var ordinal = start + i;
                var oldStatementSpan = oldText.Lines.GetTextSpanSafe(oldActiveStatements[ordinal].Span);

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

                    lambda.ActiveNodeIndices.Add(i);
                }

                SyntaxNode trackedNode = null;
                // Tracking spans corresponding to the active statements from the tracking service.
                // We seed the method body matching algorithm with tracking spans (unless they were deleted)
                // to get precise matching.
                TextSpan trackedSpan = default;
                var isTracked = trackingServiceOpt?.TryGetSpan(new ActiveStatementId(documentId, ordinal), newText, out trackedSpan) ?? false;

                if (isTracked)
                {
                    // The tracking span might have been deleted or moved outside of the member span.
                    // It is not an error to move the statement - we just ignore it.
                    if (trackedSpan.Length != 0 && edit.NewNode.Span.Contains(trackedSpan))
                    {
                        var newStatementSyntax = FindStatement(newBody, trackedSpan, out var part);
                        Contract.ThrowIfNull(newStatementSyntax);

                        var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody, newStatementSyntax);

                        // The tracking span might have been moved outside of the lambda span.
                        // It is not an error to move the statement - we just ignore it.
                        if (oldEnclosingLambdaBody == newEnclosingLambdaBody &&
                            StatementLabelEquals(oldStatementSyntax, newStatementSyntax))
                        {
                            trackedNode = newStatementSyntax;
                        }
                    }
                }

                activeNodes[i] = new ActiveNode(oldStatementSyntax, oldEnclosingLambdaBody, statementPart, isTracked ? trackedSpan : (TextSpan?)null, trackedNode);
            }

            var bodyMatch = ComputeBodyMatch(oldBody, newBody, activeNodes.Where(n => n.EnclosingLambdaBodyOpt == null).ToArray(), diagnostics, out var oldHasStateMachineSuspensionPoint, out var newHasStateMachineSuspensionPoint);
            var map = ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics);

            // Save the body match for local variable mapping.
            // We'll use it to tell the compiler what local variables to preserve in an active method.
            // An edited async/iterator method is considered active.
            updatedMembers.Add(new UpdatedMemberInfo(editOrdinal, oldBody, newBody, map, lazyActiveOrMatchedLambdas, hasActiveStatement, oldHasStateMachineSuspensionPoint, newHasStateMachineSuspensionPoint));

            for (var i = 0; i < activeNodes.Length; i++)
            {
                var ordinal = start + i;
                var hasMatching = false;
                var isNonLeaf = oldActiveStatements[ordinal].IsNonLeaf;
                var isPartiallyExecuted = (oldActiveStatements[ordinal].Flags & ActiveStatementFlags.PartiallyExecuted) != 0;
                var statementPart = activeNodes[i].StatementPart;
                var oldStatementSyntax = activeNodes[i].OldNode;
                var oldEnclosingLambdaBody = activeNodes[i].EnclosingLambdaBodyOpt;

                newExceptionRegions[ordinal] = ImmutableArray.Create<LinePositionSpan>();

                TextSpan newSpan;
                SyntaxNode newStatementSyntaxOpt;
                Match<SyntaxNode> match;

                if (oldEnclosingLambdaBody == null)
                {
                    match = bodyMatch;

                    hasMatching = TryMatchActiveStatement(oldStatementSyntax, statementPart, oldBody, newBody, out newStatementSyntaxOpt) ||
                                  match.TryGetNewNode(oldStatementSyntax, out newStatementSyntaxOpt);
                }
                else
                {
                    var oldLambdaInfo = lazyActiveOrMatchedLambdas[oldEnclosingLambdaBody];
                    var newEnclosingLambdaBody = oldLambdaInfo.NewBody;
                    match = oldLambdaInfo.Match;

                    if (match != null)
                    {
                        hasMatching = TryMatchActiveStatement(oldStatementSyntax, statementPart, oldEnclosingLambdaBody, newEnclosingLambdaBody, out newStatementSyntaxOpt) ||
                                      match.TryGetNewNode(oldStatementSyntax, out newStatementSyntaxOpt);
                    }
                    else
                    {
                        // Lambda match is null if lambdas can't be matched, 
                        // in such case we won't have active statement matched either.
                        hasMatching = false;
                        newStatementSyntaxOpt = null;
                    }
                }

                if (hasMatching)
                {
                    Debug.Assert(newStatementSyntaxOpt != null);

                    // The matching node doesn't produce sequence points.
                    // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
                    newSpan = FindClosestActiveSpan(newStatementSyntaxOpt, statementPart);

                    if ((isNonLeaf || isPartiallyExecuted) && !AreEquivalentActiveStatements(oldStatementSyntax, newStatementSyntaxOpt, statementPart))
                    {
                        // rude edit: non-leaf active statement changed
                        diagnostics.Add(new RudeEditDiagnostic(isNonLeaf ? RudeEditKind.ActiveStatementUpdate : RudeEditKind.PartiallyExecutedActiveStatementUpdate, newSpan));
                    }

                    // other statements around active statement:
                    ReportOtherRudeEditsAroundActiveStatement(diagnostics, match, oldStatementSyntax, newStatementSyntaxOpt, isNonLeaf);
                }
                else if (match == null)
                {
                    Debug.Assert(oldEnclosingLambdaBody != null);

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
                            GetDeletedNodeDiagnosticSpan(match.Matches, oldStatementSyntax)));
                    }
                }

                // exception handling around the statement:
                CalculateExceptionRegionsAroundActiveStatement(
                    bodyMatch,
                    oldStatementSyntax,
                    newStatementSyntaxOpt,
                    newSpan,
                    ordinal,
                    newText,
                    isNonLeaf,
                    newExceptionRegions,
                    diagnostics);

                Debug.Assert(newActiveStatements[ordinal] == null && newSpan != default);

                newActiveStatements[ordinal] = oldActiveStatements[ordinal].WithSpan(newText.Lines.GetLinePositionSpan(newSpan));

                // Update tracking span if we found a matching active statement whose span is different.
                // It could have been deleted or moved out of the method/lambda body, in which case we set it to empty.
                if (activeNodes[i].TrackedSpanOpt.HasValue && activeNodes[i].TrackedSpanOpt.Value != newSpan)
                {
                    updatedTrackingSpans.Add((new ActiveStatementId(documentId, ordinal), new ActiveStatementTextSpan(oldActiveStatements[ordinal].Flags, newSpan)));
                }
            }
        }

        private void CalculateExceptionRegionsAroundActiveStatement(
            Match<SyntaxNode> bodyMatch,
            SyntaxNode oldStatementSyntax,
            SyntaxNode newStatementSyntaxOpt,
            TextSpan newStatementSyntaxSpan,
            int ordinal,
            SourceText newText,
            bool isNonLeaf,
            ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            List<RudeEditDiagnostic> diagnostics)
        {
            if (newStatementSyntaxOpt == null && bodyMatch.NewRoot.Span.Contains(newStatementSyntaxSpan.Start))
            {
                newStatementSyntaxOpt = bodyMatch.NewRoot.FindToken(newStatementSyntaxSpan.Start).Parent;
            }

            if (newStatementSyntaxOpt == null)
            {
                return;
            }

            var oldAncestors = GetExceptionHandlingAncestors(oldStatementSyntax, isNonLeaf);
            var newAncestors = GetExceptionHandlingAncestors(newStatementSyntaxOpt, isNonLeaf);

            if (oldAncestors.Count > 0 || newAncestors.Count > 0)
            {
                var edits = bodyMatch.GetSequenceEdits(oldAncestors, newAncestors);
                ReportEnclosingExceptionHandlingRudeEdits(diagnostics, edits, oldStatementSyntax, newStatementSyntaxSpan);

                // Exception regions are not needed in presence of errors.
                if (diagnostics.Count == 0)
                {
                    Debug.Assert(oldAncestors.Count == newAncestors.Count);
                    newExceptionRegions[ordinal] = GetExceptionRegions(newAncestors, newText);
                }
            }
        }

        /// <summary>
        /// Calculates a syntax map of the entire method body including all lambda bodies it contains (recursively).
        /// </summary>
        private BidirectionalMap<SyntaxNode> ComputeMap(
            Match<SyntaxNode> bodyMatch,
            ActiveNode[] activeNodes,
            ref Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas,
            List<RudeEditDiagnostic> diagnostics)
        {
            ArrayBuilder<Match<SyntaxNode>> lambdaBodyMatches = null;
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
                    // Body match of a lambda whose body is an expression has the lambda as a root.
                    // The lambda has already been included when enumerating parent body matches.
                    Debug.Assert(
                        !map.ContainsKey(pair.Key) ||
                        pair.Key == lambdaBodyMatch.OldRoot && pair.Value == lambdaBodyMatch.NewRoot && IsLambda(pair.Key) && IsLambda(pair.Value));

                    map[pair.Key] = pair.Value;
                    reverseMap[pair.Value] = pair.Key;
                }
            }

            lambdaBodyMatches?.Free();

            return new BidirectionalMap<SyntaxNode>(map, reverseMap);
        }

        private Match<SyntaxNode> ComputeLambdaBodyMatch(
            SyntaxNode oldLambdaBody,
            SyntaxNode newLambdaBody,
            ActiveNode[] activeNodes,
            [Out]Dictionary<SyntaxNode, LambdaInfo> activeOrMatchedLambdas,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            ActiveNode[] activeNodesInLambda;
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
            List<RudeEditDiagnostic> diagnostics,
            out bool oldHasStateMachineSuspensionPoint,
            out bool newHasStateMachineSuspensionPoint)
        {
            Debug.Assert(oldBody != null);
            Debug.Assert(newBody != null);
            Debug.Assert(activeNodes != null);
            Debug.Assert(diagnostics != null);

            List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches = null;
            List<SequenceEdit> lazyRudeEdits = null;
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

        internal virtual void ReportStateMachineSuspensionPointDeletedRudeEdit(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode deletedSuspensionPoint)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.Delete,
                GetDeletedNodeDiagnosticSpan(match.Matches, deletedSuspensionPoint),
                deletedSuspensionPoint,
                new[] { GetSuspensionPointDisplayName(deletedSuspensionPoint, EditKind.Delete) }));
        }

        internal virtual void ReportStateMachineSuspensionPointInsertedRudeEdit(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode insertedSuspensionPoint, bool aroundActiveStatement)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                aroundActiveStatement ? RudeEditKind.InsertAroundActiveStatement : RudeEditKind.Insert,
                GetDiagnosticSpan(insertedSuspensionPoint, EditKind.Insert),
                insertedSuspensionPoint,
                new[] { GetSuspensionPointDisplayName(insertedSuspensionPoint, EditKind.Insert) }));
        }

        private static void AddMatchingActiveNodes(ref List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches, IEnumerable<ActiveNode> activeNodes)
        {
            // add nodes that are tracked by the editor buffer to known matches:
            foreach (var activeNode in activeNodes)
            {
                if (activeNode.NewTrackedNodeOpt != null)
                {
                    if (lazyKnownMatches == null)
                    {
                        lazyKnownMatches = new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
                    }

                    lazyKnownMatches.Add(KeyValuePairUtil.Create(activeNode.OldNode, activeNode.NewTrackedNodeOpt));
                }
            }
        }

        private void AddMatchingStateMachineSuspensionPoints(
            ref List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches,
            ref List<SequenceEdit> lazyRudeEdits,
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

        public ImmutableArray<LinePositionSpan> GetExceptionRegions(SourceText text, SyntaxNode syntaxRoot, LinePositionSpan activeStatementSpan, bool isNonLeaf, out bool isCovered)
        {
            var textSpan = text.Lines.GetTextSpanSafe(activeStatementSpan);
            var token = syntaxRoot.FindToken(textSpan.Start);
            var ancestors = GetExceptionHandlingAncestors(token.Parent, isNonLeaf);
            return GetExceptionRegions(ancestors, text, out isCovered);
        }

        private ImmutableArray<LinePositionSpan> GetExceptionRegions(List<SyntaxNode> exceptionHandlingAncestors, SourceText text)
            => GetExceptionRegions(exceptionHandlingAncestors, text, out _);

        private ImmutableArray<LinePositionSpan> GetExceptionRegions(List<SyntaxNode> exceptionHandlingAncestors, SourceText text, out bool isCovered)
        {
            isCovered = false;

            if (exceptionHandlingAncestors.Count == 0)
            {
                return ImmutableArray.Create<LinePositionSpan>();
            }

            var result = ArrayBuilder<LinePositionSpan>.GetInstance();

            for (var i = exceptionHandlingAncestors.Count - 1; i >= 0; i--)
            {
                var span = GetExceptionHandlingRegion(exceptionHandlingAncestors[i], out var coversAllChildren);

                result.Add(text.Lines.GetLinePositionSpan(span));

                // Exception regions describe regions of code that can't be edited.
                // If the span covers all the children nodes we don't need to descend further.
                if (coversAllChildren)
                {
                    isCovered = true;
                    break;
                }
            }

            return result.ToImmutableAndFree();
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
            foreach (var nodeAndPart in EnumerateNearStatements(deletedNode))
            {
                var oldNode = nodeAndPart.Key;
                var part = nodeAndPart.Value;
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
            Debug.Assert(hasAncestor);
            return GetDiagnosticSpan(newAncestor, EditKind.Delete);
        }

        /// <summary>
        /// Finds the inner-most ancestor of the specified node that has a matching node in the new tree.
        /// </summary>
        private static bool TryGetMatchingAncestor(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode oldNode, out SyntaxNode newAncestor)
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

        protected virtual bool TryGetOverlappingActiveStatements(
            SourceText baseText,
            TextSpan declarationSpan,
            ImmutableArray<ActiveStatement> statements,
            out int start,
            out int end)
        {
            var lines = baseText.Lines;

            // TODO (tomat): use BinarySearch

            var i = 0;
            while (i < statements.Length && !declarationSpan.OverlapsWith(lines.GetTextSpanSafe(statements[i].Span)))
            {
                i++;
            }

            if (i == statements.Length)
            {
                start = end = -1;
                return false;
            }

            start = i;
            i++;

            while (i < statements.Length && declarationSpan.OverlapsWith(lines.GetTextSpanSafe(statements[i].Span)))
            {
                i++;
            }

            end = i;
            return true;
        }

        protected static bool HasParentEdit(Dictionary<SyntaxNode, EditKind> editMap, Edit<SyntaxNode> edit)
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

        protected static bool HasEdit(Dictionary<SyntaxNode, EditKind> editMap, SyntaxNode node, EditKind editKind)
        {
            return editMap.TryGetValue(node, out var parentEdit) && parentEdit == editKind;
        }

        #endregion

        #region Rude Edits around Active Statement 

        protected void AddAroundActiveStatementRudeDiagnostic(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode, TextSpan newActiveStatementSpan)
        {
            if (oldNode == null)
            {
                AddRudeInsertAroundActiveStatement(diagnostics, newNode);
            }
            else if (newNode == null)
            {
                AddRudeDeleteAroundActiveStatement(diagnostics, oldNode, newActiveStatementSpan);
            }
            else
            {
                AddRudeUpdateAroundActiveStatement(diagnostics, newNode);
            }
        }

        protected void AddRudeUpdateAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.UpdateAroundActiveStatement,
                GetDiagnosticSpan(newNode, EditKind.Update),
                newNode,
                new[] { GetDisplayName(newNode, EditKind.Update) }));
        }

        protected void AddRudeInsertAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.InsertAroundActiveStatement,
                GetDiagnosticSpan(newNode, EditKind.Insert),
                newNode,
                new[] { GetDisplayName(newNode, EditKind.Insert) }));
        }

        protected void AddRudeDeleteAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, TextSpan newActiveStatementSpan)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.DeleteAroundActiveStatement,
                newActiveStatementSpan,
                oldNode,
                new[] { GetDisplayName(oldNode, EditKind.Delete) }));
        }

        protected void ReportUnmatchedStatements<TSyntaxNode>(
            List<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            Func<SyntaxNode, bool> nodeSelector,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement,
            Func<TSyntaxNode, TSyntaxNode, bool> areEquivalent,
            Func<TSyntaxNode, TSyntaxNode, bool> areSimilar)
            where TSyntaxNode : SyntaxNode
        {
            List<SyntaxNode> oldNodes = null, newNodes = null;
            GetAncestors(GetEncompassingAncestor(match.OldRoot), oldActiveStatement, nodeSelector, ref oldNodes);
            GetAncestors(GetEncompassingAncestor(match.NewRoot), newActiveStatement, nodeSelector, ref newNodes);

            if (newNodes != null)
            {
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
        }

        private void ReportRudeEditsAndInserts(List<SyntaxNode> oldNodes, List<SyntaxNode> newNodes, List<RudeEditDiagnostic> diagnostics)
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
                    if (i < oldNodeCount && oldNodes[i] != null)
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
            List<SyntaxNode> oldNodes,
            List<SyntaxNode> newNodes,
            List<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
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

                SyntaxNode oldNode;
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

        private static int IndexOfEquivalent<TSyntaxNode>(SyntaxNode newNode, List<SyntaxNode> oldNodes, int startIndex, Func<TSyntaxNode, TSyntaxNode, bool> comparer)
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

        private static void GetAncestors(SyntaxNode root, SyntaxNode node, Func<SyntaxNode, bool> nodeSelector, ref List<SyntaxNode> list)
        {
            while (node != root)
            {
                if (nodeSelector(node))
                {
                    if (list == null)
                    {
                        list = new List<SyntaxNode>();
                    }

                    list.Add(node);
                }

                node = node.Parent;
            }

            if (list != null)
            {
                list.Reverse();
            }
        }

        #endregion

        #region Trivia Analysis

        private void AnalyzeTrivia(
            SourceText oldSource,
            SourceText newSource,
            Match<SyntaxNode> topMatch,
            Dictionary<SyntaxNode, EditKind> editMap,
            [Out]List<(SyntaxNode OldNode, SyntaxNode NewNode)> triviaEdits,
            [Out]List<LineChange> lineEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            foreach (var (oldNode, newNode) in topMatch.Matches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (editMap.ContainsKey(newNode))
                {
                    // Updated or inserted members will be (re)generated and don't need line edits.
                    Debug.Assert(editMap[newNode] == EditKind.Update || editMap[newNode] == EditKind.Insert);
                    continue;
                }

                var newTokens = TryGetActiveTokens(newNode);
                if (newTokens == null)
                {
                    continue;
                }

                var newTokensEnum = newTokens.GetEnumerator();
                var oldTokensEnum = TryGetActiveTokens(oldNode).GetEnumerator();

                var oldLines = oldSource.Lines;
                var newLines = newSource.Lines;

                // If line and column position of all tokens in the body change by the same delta
                // we add a line delta to the line edits. Otherwise we assume that some sequence points 
                // in the body might have changed their source spans and thus require recompilation 
                // of the method. 
                // 
                // This approach requires recompilation for more methods then really necessary. 
                // The debugger APIs allow to pass line deltas for each sequence point that moved.
                // Whenever we detect a change in line position we can check if it actually affects any 
                // sequence point (breakpoint span). If not we can insert a line delta for that change.
                // However to the user it would seem arbitrary that a trivia edit in otherwise uneditable method
                // (e.g. generic method) sometimes succeeds. 
                //
                // We could still consider checking sequence points as an optimization to avoid recompiling 
                // editable methods just because some trivia was changed in between breakpoint spans.

                var previousNewToken = default(SyntaxToken);
                var requiresUpdate = false;
                var isFirstToken = true;
                var firstTokenLineDelta = 0;
                LineChange firstTokenLineChange = default;
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
                        break;
                    }

                    var oldStart = oldTokensEnum.Current.SpanStart;
                    var newStart = newTokensEnum.Current.SpanStart;

                    var oldPosition = oldLines.GetLinePosition(oldStart);
                    var newPosition = newLines.GetLinePosition(newStart);

                    if (oldPosition.Character != newPosition.Character)
                    {
                        requiresUpdate = true;
                        break;
                    }

                    var lineDelta = oldPosition.Line - newPosition.Line;
                    if (isFirstToken)
                    {
                        isFirstToken = false;
                        firstTokenLineDelta = lineDelta;
                        firstTokenLineChange = (lineDelta != 0) ? new LineChange(oldPosition.Line, newPosition.Line) : default;
                    }
                    else if (firstTokenLineDelta != lineDelta)
                    {
                        requiresUpdate = true;
                        break;
                    }

                    previousNewToken = newTokensEnum.Current;
                }

                if (requiresUpdate)
                {
                    triviaEdits.Add((oldNode, newNode));

                    var currentToken = newTokensEnum.Current;

                    var triviaSpan = TextSpan.FromBounds(
                        previousNewToken.HasTrailingTrivia ? previousNewToken.Span.End : currentToken.FullSpan.Start,
                        currentToken.SpanStart);

                    ReportMemberUpdateRudeEdits(diagnostics, newNode, triviaSpan);
                }
                else if (firstTokenLineDelta != 0)
                {
                    lineEdits.Add(firstTokenLineChange);
                }
            }

            lineEdits.Sort(CompareLineChanges);
        }

        private static int CompareLineChanges(LineChange x, LineChange y)
        {
            return x.OldLine.CompareTo(y.OldLine);
        }

        #endregion

        #region Semantic Analysis

        private sealed class AssemblyEqualityComparer : IEqualityComparer<IAssemblySymbol>
        {
            public static readonly IEqualityComparer<IAssemblySymbol> Instance = new AssemblyEqualityComparer();

            public bool Equals(IAssemblySymbol x, IAssemblySymbol y)
            {
                // Types defined in old source assembly need to be treated as equivalent to types in the new source assembly,
                // provided that they only differ in their containing assemblies.
                // 
                // The old source symbol has the same identity as the new one.
                // Two distinct assembly symbols that are referenced by the compilations have to have distinct identities.
                // If the compilation has two metadata references whose identities unify the compiler de-dups them and only creates
                // a single PE symbol. Thus comparing assemblies by identity partitions them so that each partition
                // contains assemblies that originated from the same Gen0 assembly.

                return x.Identity.Equals(y.Identity);
            }

            public int GetHashCode(IAssemblySymbol obj)
            {
                return obj.Identity.GetHashCode();
            }
        }

        protected static readonly SymbolEquivalenceComparer s_assemblyEqualityComparer = new SymbolEquivalenceComparer(AssemblyEqualityComparer.Instance, distinguishRefFromOut: true);

        protected static bool SignaturesEquivalent(ImmutableArray<IParameterSymbol> oldParameters, ITypeSymbol oldReturnType, ImmutableArray<IParameterSymbol> newParameters, ITypeSymbol newReturnType)
        {
            return oldParameters.SequenceEqual(newParameters, s_assemblyEqualityComparer.ParameterEquivalenceComparer) &&
                   s_assemblyEqualityComparer.Equals(oldReturnType, newReturnType);
        }

        protected static bool MemberSignaturesEquivalent(
            ISymbol oldMemberOpt,
            ISymbol newMemberOpt,
            Func<ImmutableArray<IParameterSymbol>, ITypeSymbol, ImmutableArray<IParameterSymbol>, ITypeSymbol, bool> signatureComparer = null)
        {
            if (oldMemberOpt == newMemberOpt)
            {
                return true;
            }

            if (oldMemberOpt == null || newMemberOpt == null || oldMemberOpt.Kind != newMemberOpt.Kind)
            {
                return false;
            }

            if (signatureComparer == null)
            {
                signatureComparer = SignaturesEquivalent;
            }

            switch (oldMemberOpt.Kind)
            {
                case SymbolKind.Field:
                    var oldField = (IFieldSymbol)oldMemberOpt;
                    var newField = (IFieldSymbol)newMemberOpt;
                    return signatureComparer(ImmutableArray<IParameterSymbol>.Empty, oldField.Type, ImmutableArray<IParameterSymbol>.Empty, newField.Type);

                case SymbolKind.Property:
                    var oldProperty = (IPropertySymbol)oldMemberOpt;
                    var newProperty = (IPropertySymbol)newMemberOpt;
                    return signatureComparer(oldProperty.Parameters, oldProperty.Type, newProperty.Parameters, newProperty.Type);

                case SymbolKind.Method:
                    var oldMethod = (IMethodSymbol)oldMemberOpt;
                    var newMethod = (IMethodSymbol)newMemberOpt;
                    return signatureComparer(oldMethod.Parameters, oldMethod.ReturnType, newMethod.Parameters, newMethod.ReturnType);

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldMemberOpt.Kind);
            }
        }

        private readonly struct ConstructorEdit
        {
            public readonly INamedTypeSymbol OldType;

            // { new field/property initializer or constructor declaration -> syntax map }
            public readonly Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> ChangedDeclarations;

            public ConstructorEdit(INamedTypeSymbol oldType)
            {
                Debug.Assert(oldType != null);

                OldType = oldType;
                ChangedDeclarations = new Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>>();
            }
        }

        private void AnalyzeSemantics(
            EditScript<SyntaxNode> editScript,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            ImmutableArray<ActiveStatement> oldActiveStatements,
            List<(SyntaxNode OldNode, SyntaxNode NewNode)> triviaEdits,
            List<UpdatedMemberInfo> updatedMembers,
            SemanticModel oldModel,
            SemanticModel newModel,
            [Out]List<SemanticEdit> semanticEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            out Diagnostic firstDeclarationErrorOpt,
            CancellationToken cancellationToken)
        {
            // { new type -> constructor update }
            Dictionary<INamedTypeSymbol, ConstructorEdit> instanceConstructorEdits = null;
            Dictionary<INamedTypeSymbol, ConstructorEdit> staticConstructorEdits = null;

            INamedTypeSymbol layoutAttribute = null;
            var newSymbolsWithEdit = new HashSet<ISymbol>();
            var updatedMemberIndex = 0;
            firstDeclarationErrorOpt = null;
            for (var i = 0; i < editScript.Edits.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var edit = editScript.Edits[i];

                ISymbol oldSymbol, newSymbol;
                Func<SyntaxNode, SyntaxNode> syntaxMapOpt;
                SemanticEditKind editKind;

                switch (edit.Kind)
                {
                    case EditKind.Move:
                        // Move is always a Rude Edit.
                        throw ExceptionUtilities.UnexpectedValue(edit.Kind);

                    case EditKind.Delete:
                        {
                            editKind = SemanticEditKind.Delete;

                            if (HasParentEdit(editMap, edit))
                            {
                                continue;
                            }

                            oldSymbol = GetSymbolForEdit(oldModel, edit.OldNode, edit.Kind, editMap, cancellationToken);
                            if (oldSymbol == null)
                            {
                                continue;
                            }

                            // The only member that is allowed to be deleted is a parameterless constructor. 
                            // For any other member a rude edit is reported earlier during syntax edit classification.
                            // Deleting a parameterless constructor needs special handling.
                            // If the new type has a parameterless ctor of the same accessibility then UPDATE.
                            // Error otherwise.

                            Debug.Assert(AsParameterlessConstructor(oldSymbol) != null);

                            var oldTypeSyntax = TryGetContainingTypeDeclaration(edit.OldNode);
                            Debug.Assert(oldTypeSyntax != null);

                            var newType = TryGetPartnerType(oldTypeSyntax, editScript.Match, newModel, cancellationToken);

                            newSymbol = TryGetParameterlessConstructor(newType, oldSymbol.IsStatic);
                            if (newSymbol == null || newSymbol.DeclaredAccessibility != oldSymbol.DeclaredAccessibility)
                            {
                                diagnostics.Add(new RudeEditDiagnostic(
                                    RudeEditKind.Delete,
                                    GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, edit.OldNode),
                                    edit.OldNode,
                                    new[] { GetDisplayName(edit.OldNode, EditKind.Delete) }));

                                continue;
                            }

                            editKind = SemanticEditKind.Update;
                            syntaxMapOpt = null;
                        }

                        break;

                    case EditKind.Reorder:
                        // Currently we don't do any semantic checks for reordering
                        // and we don't need to report them to the compiler either.

                        // Reordering of fields is not allowed since it changes the layout of the type.
                        Debug.Assert(!IsDeclarationWithInitializer(edit.OldNode) && !IsDeclarationWithInitializer(edit.NewNode));
                        continue;

                    case EditKind.Insert:
                        {
                            editKind = SemanticEditKind.Insert;

                            var newTypeSyntax = TryGetContainingTypeDeclaration(edit.NewNode);

                            if (newTypeSyntax != null && HasEdit(editMap, newTypeSyntax, EditKind.Insert))
                            {
                                // inserting into a new type
                                continue;
                            }

                            syntaxMapOpt = null;
                            oldSymbol = null;
                            newSymbol = GetSymbolForEdit(newModel, edit.NewNode, edit.Kind, editMap, cancellationToken);

                            if (newSymbol == null)
                            {
                                // node doesn't represent a symbol
                                continue;
                            }

                            // TODO: scripting
                            // inserting a top-level member/type
                            if (newTypeSyntax == null)
                            {
                                break;
                            }

                            var newType = (INamedTypeSymbol)newModel.GetDeclaredSymbol(newTypeSyntax, cancellationToken);
                            var oldType = TryGetPartnerType(newTypeSyntax, editScript.Match, oldModel, cancellationToken);

                            // There has to be a matching old type syntax since the containing type hasn't been inserted.
                            Debug.Assert(oldType != null);
                            Debug.Assert(newType != null);

                            // Validate that the type declarations are correct. If not we can't reason about their members.
                            // Declaration diagnostics are cached on compilation, so we don't need to cache them here.
                            firstDeclarationErrorOpt =
                                GetFirstDeclarationError(oldModel, oldType, cancellationToken) ??
                                GetFirstDeclarationError(newModel, newType, cancellationToken);

                            if (firstDeclarationErrorOpt != null)
                            {
                                continue;
                            }

                            // Inserting a parameterless constructor needs special handling:
                            // 1) static ctor
                            //    a) old type has an implicit static ctor
                            //       UPDATE of the implicit static ctor
                            //    b) otherwise
                            //       INSERT of a static parameterless ctor
                            // 
                            // 2) public instance ctor
                            //    a) old type has an implicit instance ctor
                            //       UPDATE of the implicit instance ctor
                            //    b) otherwise
                            //       ERROR: adding a non-private member
                            // 3) non-public instance ctor
                            //    a) old type has an implicit instance ctor
                            //       ERROR: changing visibility of the ctor
                            //    b) otherwise
                            //       INSERT of an instance parameterless ctor

                            var newCtor = AsParameterlessConstructor(newSymbol);
                            if (newCtor != null)
                            {
                                oldSymbol = TryGetParameterlessConstructor(oldType, newSymbol.IsStatic);

                                if (newCtor.IsStatic)
                                {
                                    if (oldSymbol != null)
                                    {
                                        editKind = SemanticEditKind.Update;
                                    }
                                }
                                else if (oldSymbol != null)
                                {
                                    if (oldSymbol.DeclaredAccessibility != newCtor.DeclaredAccessibility)
                                    {
                                        // changing visibility of a member
                                        diagnostics.Add(new RudeEditDiagnostic(
                                            RudeEditKind.ChangingConstructorVisibility,
                                            GetDiagnosticSpan(edit.NewNode, EditKind.Insert)));
                                    }
                                    else
                                    {
                                        editKind = SemanticEditKind.Update;
                                    }
                                }
                            }

                            if (editKind == SemanticEditKind.Insert)
                            {
                                ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol);
                                ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, edit.NewNode, newModel, ref layoutAttribute);
                            }

                            bool isConstructorWithMemberInitializers;
                            if ((isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(edit.NewNode)) ||
                                IsDeclarationWithInitializer(edit.NewNode))
                            {
                                if (DeferConstructorEdit(
                                    oldType,
                                    newType,
                                    editKind,
                                    edit.NewNode,
                                    newSymbol,
                                    newModel,
                                    isConstructorWithMemberInitializers,
                                    ref syntaxMapOpt,
                                    ref instanceConstructorEdits,
                                    ref staticConstructorEdits,
                                    diagnostics,
                                    cancellationToken))
                                {
                                    if (newSymbol.Kind == SymbolKind.Method)
                                    {
                                        // Don't add a separate semantic edit for a field/property with an initializer.
                                        // All edits of initializers will be aggregated to edits of constructors where these initializers are emitted.
                                        continue;
                                    }
                                    else
                                    {
                                        // A semantic edit to create the field/property is gonna be added.
                                        Debug.Assert(editKind == SemanticEditKind.Insert);
                                    }
                                }
                            }
                        }

                        break;

                    case EditKind.Update:
                        {
                            editKind = SemanticEditKind.Update;

                            newSymbol = GetSymbolForEdit(newModel, edit.NewNode, edit.Kind, editMap, cancellationToken);
                            if (newSymbol == null)
                            {
                                // node doesn't represent a symbol
                                continue;
                            }

                            oldSymbol = GetSymbolForEdit(oldModel, edit.OldNode, edit.Kind, editMap, cancellationToken);
                            Debug.Assert((newSymbol == null) == (oldSymbol == null));

                            var oldContainingType = oldSymbol.ContainingType;
                            var newContainingType = newSymbol.ContainingType;

                            // Validate that the type declarations are correct to avoid issues with invalid partial declarations, etc.
                            // Declaration diagnostics are cached on compilation, so we don't need to cache them here.
                            firstDeclarationErrorOpt =
                                GetFirstDeclarationError(oldModel, oldContainingType, cancellationToken) ??
                                GetFirstDeclarationError(newModel, newContainingType, cancellationToken);

                            if (firstDeclarationErrorOpt != null)
                            {
                                continue;
                            }

                            if (updatedMemberIndex < updatedMembers.Count && updatedMembers[updatedMemberIndex].EditOrdinal == i)
                            {
                                var updatedMember = updatedMembers[updatedMemberIndex];

                                ReportStateMachineRudeEdits(oldModel.Compilation, updatedMember, oldSymbol, diagnostics);

                                ReportLambdaAndClosureRudeEdits(
                                    oldModel,
                                    updatedMember.OldBody,
                                    newModel,
                                    updatedMember.NewBody,
                                    newSymbol,
                                    updatedMember.ActiveOrMatchedLambdasOpt,
                                    updatedMember.Map,
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
                                if (updatedMember.HasActiveStatement || updatedMember.NewHasStateMachineSuspensionPoint || newBodyHasLambdas)
                                {
                                    syntaxMapOpt = CreateSyntaxMap(updatedMember.Map.Reverse);
                                }
                                else
                                {
                                    syntaxMapOpt = null;
                                }

                                updatedMemberIndex++;
                            }
                            else
                            {
                                syntaxMapOpt = null;
                            }

                            // If a constructor changes from including initializers to not including initializers
                            // we don't need to aggregate syntax map from all initializers for the constructor update semantic edit.
                            bool isConstructorWithMemberInitializers;
                            if ((isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(edit.NewNode)) ||
                                IsDeclarationWithInitializer(edit.OldNode) ||
                                IsDeclarationWithInitializer(edit.NewNode))
                            {
                                if (DeferConstructorEdit(
                                    oldContainingType,
                                    newContainingType,
                                    editKind,
                                    edit.NewNode,
                                    newSymbol,
                                    newModel,
                                    isConstructorWithMemberInitializers,
                                    ref syntaxMapOpt,
                                    ref instanceConstructorEdits,
                                    ref staticConstructorEdits,
                                    diagnostics,
                                    cancellationToken))
                                {
                                    // Don't add a separate semantic edit for a field/property with an initializer.
                                    // All edits of initializers will be aggregated to edits of constructors where these initializers are emitted.
                                    continue;
                                }
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                }

                semanticEdits.Add(new SemanticEdit(editKind, oldSymbol, newSymbol, syntaxMapOpt, preserveLocalVariables: syntaxMapOpt != null));
                newSymbolsWithEdit.Add(newSymbol);
            }

            foreach (var (oldNode, newNode) in triviaEdits)
            {
                var oldSymbol = GetSymbolForEdit(oldModel, oldNode, EditKind.Update, editMap, cancellationToken);
                var newSymbol = GetSymbolForEdit(newModel, newNode, EditKind.Update, editMap, cancellationToken);
                var oldContainingType = oldSymbol.ContainingType;
                var newContainingType = newSymbol.ContainingType;

                // Validate that the type declarations are correct to avoid issues with invalid partial declarations, etc.
                // Declaration diagnostics are cached on compilation, so we don't need to cache them here.
                firstDeclarationErrorOpt =
                    GetFirstDeclarationError(oldModel, oldContainingType, cancellationToken) ??
                    GetFirstDeclarationError(newModel, newContainingType, cancellationToken);

                if (firstDeclarationErrorOpt != null)
                {
                    continue;
                }

                // We need to provide syntax map to the compiler if the member is active (see member update above):
                var isActiveMember =
                    TryGetOverlappingActiveStatements(oldText, oldNode.Span, oldActiveStatements, out var start, out var end) ||
                    IsStateMachineMethod(oldNode) ||
                    ContainsLambda(oldNode);

                var syntaxMap = isActiveMember ? CreateSyntaxMapForEquivalentNodes(oldNode, newNode) : null;

                // only trivia changed:
                Debug.Assert(IsConstructorWithMemberInitializers(oldNode) == IsConstructorWithMemberInitializers(newNode));
                Debug.Assert(IsDeclarationWithInitializer(oldNode) == IsDeclarationWithInitializer(newNode));

                bool isConstructorWithMemberInitializers;
                if ((isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newNode)) ||
                    IsDeclarationWithInitializer(newNode))
                {
                    if (DeferConstructorEdit(
                        oldContainingType,
                        newContainingType,
                        SemanticEditKind.Update,
                        newNode,
                        newSymbol,
                        newModel,
                        isConstructorWithMemberInitializers,
                        ref syntaxMap,
                        ref instanceConstructorEdits,
                        ref staticConstructorEdits,
                        diagnostics,
                        cancellationToken))
                    {
                        // Don't add a separate semantic edit for a field/property with an initializer.
                        // All edits of initializers will be aggregated to edits of constructors where these initializers are emitted.
                        continue;
                    }
                }

                semanticEdits.Add(new SemanticEdit(SemanticEditKind.Update, oldSymbol, newSymbol, syntaxMap, isActiveMember));
                newSymbolsWithEdit.Add(newSymbol);
            }

            if (instanceConstructorEdits != null)
            {
                AddConstructorEdits(
                    instanceConstructorEdits,
                    editScript.Match,
                    oldModel,
                    newSymbolsWithEdit,
                    isStatic: false,
                    semanticEdits: semanticEdits,
                    diagnostics: diagnostics,
                    cancellationToken: cancellationToken);
            }

            if (staticConstructorEdits != null)
            {
                AddConstructorEdits(
                    staticConstructorEdits,
                    editScript.Match,
                    oldModel,
                    newSymbolsWithEdit,
                    isStatic: true,
                    semanticEdits: semanticEdits,
                    diagnostics: diagnostics,
                    cancellationToken: cancellationToken);
            }
        }

        private Diagnostic GetFirstDeclarationError(SemanticModel primaryModel, ISymbol symbol, CancellationToken cancellationToken)
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                SemanticModel model;
                if (primaryModel.SyntaxTree == syntaxReference.SyntaxTree)
                {
                    model = primaryModel;
                }
                else
                {
                    model = primaryModel.Compilation.GetSemanticModel(syntaxReference.SyntaxTree, ignoreAccessibility: false);
                }

                var diagnostics = model.GetDeclarationDiagnostics(syntaxReference.Span, cancellationToken);
                var firstError = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
                if (firstError != null)
                {
                    return firstError;
                }
            }

            return null;
        }

        #region Type Layout Update Validation 

        internal void ReportTypeLayoutUpdateRudeEdits(
            List<RudeEditDiagnostic> diagnostics,
            ISymbol newSymbol,
            SyntaxNode newSyntax,
            SemanticModel newModel,
            ref INamedTypeSymbol layoutAttribute)
        {
            switch (newSymbol.Kind)
            {
                case SymbolKind.Field:
                    if (HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref layoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;

                case SymbolKind.Property:
                    if (HasBackingField(newSyntax) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref layoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;

                case SymbolKind.Event:
                    if (HasBackingField((IEventSymbol)newSymbol) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref layoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;
            }
        }

        private void ReportTypeLayoutUpdateRudeEdits(List<RudeEditDiagnostic> diagnostics, ISymbol symbol, SyntaxNode syntax)
        {
            var intoStruct = symbol.ContainingType.TypeKind == TypeKind.Struct;

            diagnostics.Add(new RudeEditDiagnostic(
                intoStruct ? RudeEditKind.InsertIntoStruct : RudeEditKind.InsertIntoClassWithLayout,
                syntax.Span,
                syntax,
                new[]
                {
                    GetDisplayName(syntax, EditKind.Insert),
                    GetDisplayName(TryGetContainingTypeDeclaration(syntax), EditKind.Update)
                }));
        }

        private static bool HasBackingField(IEventSymbol @event)
        {
            return @event.AddMethod.IsImplicitlyDeclared
                && !@event.IsAbstract;
        }

        // TODO: the compiler should expose TypeLayout property on INamedTypeSymbol
        private static bool HasExplicitOrSequentialLayout(
            INamedTypeSymbol type,
            SemanticModel model,
            ref INamedTypeSymbol layoutAttribute)
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

            if (layoutAttribute == null)
            {
                layoutAttribute = model.Compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName);
                if (layoutAttribute == null)
                {
                    return false;
                }
            }

            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass.Equals(layoutAttribute) && attribute.ConstructorArguments.Length == 1)
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

        private INamedTypeSymbol TryGetPartnerType(SyntaxNode typeSyntax, Match<SyntaxNode> topMatch, SemanticModel partnerModel, CancellationToken cancellationToken)
        {
            SyntaxNode partner;
            if (topMatch.OldRoot.SyntaxTree == typeSyntax.SyntaxTree)
            {
                topMatch.TryGetNewNode(typeSyntax, out partner);
            }
            else
            {
                topMatch.TryGetOldNode(typeSyntax, out partner);
            }

            if (partner == null)
            {
                return null;
            }

            Debug.Assert(partner.SyntaxTree == partnerModel.SyntaxTree);

            return (INamedTypeSymbol)partnerModel.GetDeclaredSymbol(partner, cancellationToken);
        }

        private Func<SyntaxNode, SyntaxNode> CreateSyntaxMapForEquivalentNodes(SyntaxNode oldRoot, SyntaxNode newRoot)
        {
            AreEquivalent(newRoot, oldRoot);
            return newNode =>
            {
                if (!newRoot.FullSpan.Contains(newNode.SpanStart))
                {
                    return null;
                }

                return FindPartner(newRoot, oldRoot, newNode);
            };
        }

        private static Func<SyntaxNode, SyntaxNode> CreateSyntaxMap(IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
        {
            return newNode =>
            {
                return reverseMap.TryGetValue(newNode, out var oldNode) ? oldNode : null;
            };
        }

        private Func<SyntaxNode, SyntaxNode> CreateSyntaxMapForPartialTypeConstructor(
            INamedTypeSymbol oldType,
            INamedTypeSymbol newType,
            SemanticModel newModel,
            Func<SyntaxNode, SyntaxNode> ctorSyntaxMapOpt)
        {
            return newNode => ctorSyntaxMapOpt?.Invoke(newNode) ?? FindPartnerInMemberInitializer(newModel, newType, newNode, oldType, default);
        }

        private Func<SyntaxNode, SyntaxNode> CreateAggregateSyntaxMap(
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseTopMatches,
            IReadOnlyDictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> changedDeclarations)
        {
            return newNode =>
            {
                // containing declaration
                var newDeclaration = FindMemberDeclaration(null, newNode);
                // The node is in a field, property or constructor declaration that has been changed:
                if (changedDeclarations.TryGetValue(newDeclaration, out var syntaxMapOpt))
                {
                    // If syntax map is not available the declaration was either
                    // 1) updated but is not active
                    // 2) inserted
                    return syntaxMapOpt?.Invoke(newNode);
                }
                // The node is in a declaration that hasn't been changed:
                if (reverseTopMatches.TryGetValue(newDeclaration, out var oldDeclaration))
                {
                    return FindPartner(newDeclaration, oldDeclaration, newNode);
                }

                return null;
            };
        }

        #region Constructors and Initializers

        private static IMethodSymbol AsParameterlessConstructor(ISymbol symbol)
        {
            if (symbol.Kind != SymbolKind.Method)
            {
                return null;
            }

            var method = (IMethodSymbol)symbol;
            var kind = method.MethodKind;
            if (kind != MethodKind.Constructor && kind != MethodKind.StaticConstructor)
            {
                return null;
            }

            return method.Parameters.Length == 0 ? method : null;
        }

        private bool DeferConstructorEdit(
            INamedTypeSymbol oldType,
            INamedTypeSymbol newType,
            SemanticEditKind editKind,
            SyntaxNode newDeclaration,
            ISymbol newSymbol,
            SemanticModel newModel,
            bool isConstructor,
            ref Func<SyntaxNode, SyntaxNode> syntaxMapOpt,
            ref Dictionary<INamedTypeSymbol, ConstructorEdit> instanceConstructorEdits,
            ref Dictionary<INamedTypeSymbol, ConstructorEdit> staticConstructorEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            Debug.Assert(oldType != null);
            Debug.Assert(newType != null);

            if (IsPartial(newType))
            {
                // Since we don't calculate match across partial declarations we need to disallow
                // adding and updating fields/properties with initializers of a partial type declaration.
                // Assuming this restriction we can allow editing all constructors of partial types. 
                // The ones that include initializers won't differ in the field initialization.

                if (!isConstructor)
                {
                    // rude edit: Editing a field/property initializer of a partial type.
                    diagnostics.Add(new RudeEditDiagnostic(
                                        RudeEditKind.PartialTypeInitializerUpdate,
                                        newDeclaration.Span,
                                        newDeclaration,
                                        new[] { GetDisplayName(newDeclaration, EditKind.Update) }));
                    return false;
                }

                // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                if (editKind == SemanticEditKind.Insert && HasMemberInitializerContainingLambda(oldType, newSymbol.IsStatic, cancellationToken))
                {
                    // rude edit: Adding a constructor to a type with a field or property initializer that contains an anonymous function
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                    return false;
                }

                syntaxMapOpt = CreateSyntaxMapForPartialTypeConstructor(oldType, newType, newModel, syntaxMapOpt);
                return false;
            }

            Dictionary<INamedTypeSymbol, ConstructorEdit> constructorEdits;
            if (newSymbol.IsStatic)
            {
                constructorEdits = staticConstructorEdits ??
                    (staticConstructorEdits = new Dictionary<INamedTypeSymbol, ConstructorEdit>());
            }
            else
            {
                constructorEdits = instanceConstructorEdits ??
                    (instanceConstructorEdits = new Dictionary<INamedTypeSymbol, ConstructorEdit>());
            }

            if (!constructorEdits.TryGetValue(newType, out var edit))
            {
                constructorEdits.Add(newType, edit = new ConstructorEdit(oldType));
            }

            edit.ChangedDeclarations.Add(newDeclaration, syntaxMapOpt);

            return true;
        }

        private void AddConstructorEdits(
            Dictionary<INamedTypeSymbol, ConstructorEdit> updatedTypes,
            Match<SyntaxNode> topMatch,
            SemanticModel oldModel,
            HashSet<ISymbol> newSymbolsWithEdit,
            bool isStatic,
            [Out]List<SemanticEdit> semanticEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            foreach (var updatedType in updatedTypes)
            {
                var newType = updatedType.Key;
                var update = updatedType.Value;
                var oldType = update.OldType;

                Debug.Assert(!IsPartial(oldType));
                Debug.Assert(!IsPartial(newType));

                var anyInitializerUpdates = update.ChangedDeclarations.Keys.Any(IsDeclarationWithInitializer);

                bool? lazyOldTypeHasMemberInitializerContainingLambda = null;

                foreach (var newCtor in isStatic ? newType.StaticConstructors : newType.InstanceConstructors)
                {
                    if (newSymbolsWithEdit.Contains(newCtor))
                    {
                        // we already have an edit for the new constructor
                        continue;
                    }

                    ISymbol oldCtor;
                    if (!newCtor.IsImplicitlyDeclared)
                    {
                        // Constructors have to have a single declaration syntax, they can't be partial
                        var newDeclaration = FindMemberDeclaration(null, GetSymbolSyntax(newCtor, cancellationToken));

                        // Partial types were filtered out previously and rude edits reported.
                        Debug.Assert(newDeclaration.SyntaxTree == topMatch.NewRoot.SyntaxTree);

                        // Constructor that doesn't contain initializers had a corresponding semantic edit produced previously 
                        // or was not edited. In either case we should not produce a semantic edit for it.
                        if (!IsConstructorWithMemberInitializers(newDeclaration))
                        {
                            continue;
                        }

                        // If no initializer updates were made in the type we only need to produce semantic edits for constructors
                        // whose body has been updated, otherwise we need to produce edits for all constructors that include initializers.
                        if (!anyInitializerUpdates && !update.ChangedDeclarations.ContainsKey(newDeclaration))
                        {
                            continue;
                        }

                        if (topMatch.TryGetOldNode(newDeclaration, out var oldDeclaration))
                        {
                            // If the constructor wasn't explicitly edited and its body edit is disallowed report an error.
                            var diagnosticCount = diagnostics.Count;
                            ReportMemberUpdateRudeEdits(diagnostics, newDeclaration, span: null);
                            if (diagnostics.Count > diagnosticCount)
                            {
                                continue;
                            }

                            oldCtor = oldModel.GetDeclaredSymbol(oldDeclaration, cancellationToken);
                            Debug.Assert(oldCtor != null);
                        }
                        else if (newCtor.Parameters.Length == 0)
                        {
                            oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                        }
                        else
                        {
                            // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                            if (HasMemberInitializerContainingLambda(oldType, isStatic, ref lazyOldTypeHasMemberInitializerContainingLambda, cancellationToken))
                            {
                                // rude edit: Adding a constructor to a type with a field or property initializer that contains an anonymous function
                                diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                                continue;
                            }

                            // no initializer contains lambdas => we don't need a syntax map
                            oldCtor = null;
                        }
                    }
                    else
                    {
                        oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                    }

                    // We assume here that the type is not partial and thus we collected all changed
                    // field and property initializers in update.ChangedDeclarations and we only need 
                    // the current top match to map nodes from all unchanged initializers.
                    //
                    // We will create an aggregate syntax map even in cases when we don't necessarily need it,
                    // for example if none of the edited declarations are active. It's ok to have a map that we don't need.
                    var aggregateSyntaxMap = (oldCtor != null && update.ChangedDeclarations.Count > 0) ?
                        CreateAggregateSyntaxMap(topMatch.ReverseMatches, update.ChangedDeclarations) : null;

                    semanticEdits.Add(new SemanticEdit(
                        (oldCtor == null) ? SemanticEditKind.Insert : SemanticEditKind.Update,
                        oldCtor,
                        newCtor,
                        aggregateSyntaxMap,
                        preserveLocalVariables: aggregateSyntaxMap != null));
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
                    var syntax = GetSymbolSyntax(member, cancellationToken);
                    if (IsDeclarationWithInitializer(syntax) && ContainsLambda(syntax))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ISymbol TryGetParameterlessConstructor(INamedTypeSymbol type, bool isStatic)
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

        #endregion

        #region Lambdas and Closures

        private void ReportLambdaAndClosureRudeEdits(
            SemanticModel oldModel,
            SyntaxNode oldMemberBody,
            SemanticModel newModel,
            SyntaxNode newMemberBody,
            ISymbol newMember,
            IReadOnlyDictionary<SyntaxNode, LambdaInfo> matchedLambdasOpt,
            BidirectionalMap<SyntaxNode> map,
            List<RudeEditDiagnostic> diagnostics,
            out bool newBodyHasLambdas,
            CancellationToken cancellationToken)
        {
            if (matchedLambdasOpt != null)
            {
                var anySignatureErrors = false;
                foreach (var entry in matchedLambdasOpt)
                {
                    var oldLambdaBody = entry.Key;
                    var newLambdaBody = entry.Value.NewBody;
                    ReportLambdaSignatureRudeEdits(oldModel, oldLambdaBody, newModel, newLambdaBody, diagnostics, out var hasErrors, cancellationToken);
                    anySignatureErrors |= hasErrors;
                }

                ArrayBuilder<SyntaxNode> lazyNewErroneousClauses = null;
                foreach (var entry in map.Forward)
                {
                    var oldQueryClause = entry.Key;
                    var newQueryClause = entry.Value;

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
                    newBodyHasLambdas = true;
                    return;
                }
            }

            var oldCaptures = GetCapturedVariables(oldModel, oldMemberBody);
            var newCaptures = GetCapturedVariables(newModel, newMemberBody);

            // { new capture index -> old capture index }
            var reverseCapturesMap = ArrayBuilder<int>.GetInstance(newCaptures.Length, 0);

            // { new capture index -> new closure scope or null for "this" }
            var newCapturesToClosureScopes = ArrayBuilder<SyntaxNode>.GetInstance(newCaptures.Length, null);

            // Can be calculated from other maps but it's simpler to just calculate it upfront.
            // { old capture index -> old closure scope or null for "this" }
            var oldCapturesToClosureScopes = ArrayBuilder<SyntaxNode>.GetInstance(oldCaptures.Length, null);

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
                newBodyHasLambdas = true;
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

            var oldCapturesIndex = PooledDictionary<ISymbol, int>.GetInstance();
            var newCapturesIndex = PooledDictionary<ISymbol, int>.GetInstance();

            BuildIndex(oldCapturesIndex, oldCaptures);
            BuildIndex(newCapturesIndex, newCaptures);

            if (matchedLambdasOpt != null)
            {
                var mappedLambdasHaveErrors = false;
                foreach (var entry in matchedLambdasOpt)
                {
                    var oldLambdaBody = entry.Key;
                    var newLambdaBody = entry.Value.NewBody;

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
                    newBodyHasLambdas = true;
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

            newBodyHasLambdas = false;

            var containingTypeDeclaration = TryGetContainingTypeDeclaration(newMemberBody);
            var isInInterfaceDeclaration = containingTypeDeclaration != null && IsInterfaceDeclaration(containingTypeDeclaration);

            foreach (var newLambda in newMemberBody.DescendantNodesAndSelf())
            {
                if (TryGetLambdaBodies(newLambda, out var newLambdaBody1, out var newLambdaBody2))
                {
                    if (!map.Reverse.ContainsKey(newLambda))
                    {
                        // TODO: https://github.com/dotnet/roslyn/issues/37128
                        // Local functions are emitted directly to the type containing the containing method.
                        // Although local functions are non-virtual the Core CLR currently does not support adding any method to an interface.
                        if (isInInterfaceDeclaration && IsLocalFunction(newLambda))
                        {
                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod, GetDiagnosticSpan(newLambda, EditKind.Insert), newLambda));
                        }

                        ReportMultiScopeCaptures(newLambdaBody1, newModel, newCaptures, newCaptures, newCapturesToClosureScopes, newCapturesIndex, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);

                        if (newLambdaBody2 != null)
                        {
                            ReportMultiScopeCaptures(newLambdaBody2, newModel, newCaptures, newCaptures, newCapturesToClosureScopes, newCapturesIndex, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);
                        }
                    }

                    newBodyHasLambdas = true;
                }
            }

            // Similarly for addition. We don't allow removal of lambda that has captures from multiple scopes.

            foreach (var oldLambda in oldMemberBody.DescendantNodesAndSelf())
            {
                if (TryGetLambdaBodies(oldLambda, out var oldLambdaBody1, out var oldLambdaBody2) && !map.Forward.ContainsKey(oldLambda))
                {
                    ReportMultiScopeCaptures(oldLambdaBody1, oldModel, oldCaptures, newCaptures, oldCapturesToClosureScopes, oldCapturesIndex, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);

                    if (oldLambdaBody2 != null)
                    {
                        ReportMultiScopeCaptures(oldLambdaBody2, oldModel, oldCaptures, newCaptures, oldCapturesToClosureScopes, oldCapturesIndex, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);
                    }
                }
            }

            reverseCapturesMap.Free();
            newCapturesToClosureScopes.Free();
            oldCapturesToClosureScopes.Free();
            oldCapturesIndex.Free();
            newCapturesIndex.Free();
        }

        private void ReportMultiScopeCaptures(
            SyntaxNode lambdaBody,
            SemanticModel model,
            ImmutableArray<ISymbol> captures,
            ImmutableArray<ISymbol> newCaptures,
            ArrayBuilder<SyntaxNode> newCapturesToClosureScopes,
            PooledDictionary<ISymbol, int> capturesIndex,
            ArrayBuilder<int> reverseCapturesMap,
            List<RudeEditDiagnostic> diagnostics,
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
        {
            for (var i = 0; i < array.Length; i++)
            {
                index.Add(array[i], i);
            }
        }

        protected SyntaxNode GetSymbolSyntax(ISymbol local, CancellationToken cancellationToken)
        {
            return local.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);
        }

        private TextSpan GetThisParameterDiagnosticSpan(ISymbol member)
        {
            return member.Locations.First().SourceSpan;
        }

        private TextSpan GetVariableDiagnosticSpan(ISymbol local)
        {
            // Note that in VB implicit value parameter in property setter doesn't have a location.
            // In C# its location is the location of the setter.
            // See https://github.com/dotnet/roslyn/issues/14273
            return local.Locations.FirstOrDefault()?.SourceSpan ?? local.ContainingSymbol.Locations.First().SourceSpan;
        }

        private ValueTuple<SyntaxNode, int> GetParameterKey(IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            var containingLambda = parameter.ContainingSymbol as IMethodSymbol;
            if (containingLambda?.MethodKind == MethodKind.LambdaMethod ||
                containingLambda?.MethodKind == MethodKind.LocalFunction)
            {
                var oldContainingLambdaSyntax = GetSymbolSyntax(containingLambda, cancellationToken);
                return ValueTuple.Create(oldContainingLambdaSyntax, parameter.Ordinal);
            }
            else
            {
                return ValueTuple.Create(default(SyntaxNode), parameter.Ordinal);
            }
        }

        private bool TryMapParameter(ValueTuple<SyntaxNode, int> parameterKey, IReadOnlyDictionary<SyntaxNode, SyntaxNode> map, out ValueTuple<SyntaxNode, int> mappedParameterKey)
        {
            var containingLambdaSyntax = parameterKey.Item1;
            var ordinal = parameterKey.Item2;

            if (containingLambdaSyntax == null)
            {
                // method parameter: no syntax, same ordinal (can't change since method signatures must match)
                mappedParameterKey = parameterKey;
                return true;
            }

            if (map.TryGetValue(containingLambdaSyntax, out var mappedContainingLambdaSyntax))
            {
                // parameter of an existing lambda: same ordinal (can't change since lambda signatures must match), 
                mappedParameterKey = ValueTuple.Create(mappedContainingLambdaSyntax, ordinal);
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
            [Out]ArrayBuilder<int> reverseCapturesMap,                 // {new capture index -> old capture index}
            [Out]ArrayBuilder<SyntaxNode> newCapturesToClosureScopes,  // {new capture index -> new closure scope}
            [Out]ArrayBuilder<SyntaxNode> oldCapturesToClosureScopes,  // {old capture index -> old closure scope}
            [Out]List<RudeEditDiagnostic> diagnostics,
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

            var oldLocalCapturesBySyntax = PooledDictionary<SyntaxNode, int>.GetInstance();
            var oldParameterCapturesByLambdaAndOrdinal = PooledDictionary<ValueTuple<SyntaxNode, int>, int>.GetInstance();

            for (var i = 0; i < oldCaptures.Length; i++)
            {
                var oldCapture = oldCaptures[i];

                if (oldCapture.Kind == SymbolKind.Parameter)
                {
                    oldParameterCapturesByLambdaAndOrdinal.Add(GetParameterKey((IParameterSymbol)oldCapture, cancellationToken), i);
                }
                else
                {
                    oldLocalCapturesBySyntax.Add(GetSymbolSyntax(oldCapture, cancellationToken), i);
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
                    var newCaptureSyntax = GetSymbolSyntax(newCapture, cancellationToken);

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

                if (!s_assemblyEqualityComparer.Equals(oldTypeOpt, newTypeOpt))
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
                foreach (var entry in oldParameterCapturesByLambdaAndOrdinal)
                {
                    var ordinal = entry.Key.Item2;
                    var oldContainingLambdaSyntax = entry.Key.Item1;
                    var oldCaptureIndex = entry.Value;
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

            oldLocalCapturesBySyntax.Free();
        }

        protected virtual void ReportLambdaSignatureRudeEdits(
            SemanticModel oldModel,
            SyntaxNode oldLambdaBody,
            SemanticModel newModel,
            SyntaxNode newLambdaBody,
            List<RudeEditDiagnostic> diagnostics,
            out bool hasErrors,
            CancellationToken cancellationToken)
        {
            var newLambda = GetLambda(newLambdaBody);
            var oldLambda = GetLambda(oldLambdaBody);

            Debug.Assert(IsNestedFunction(newLambda) == IsNestedFunction(oldLambda));

            // queries are analyzed separately
            if (!IsNestedFunction(newLambda))
            {
                hasErrors = false;
                return;
            }

            var oldLambdaSymbol = GetLambdaExpressionSymbol(oldModel, oldLambda, cancellationToken);
            var newLambdaSymbol = GetLambdaExpressionSymbol(newModel, newLambda, cancellationToken);

            RudeEditKind rudeEdit;

            if (!oldLambdaSymbol.Parameters.SequenceEqual(newLambdaSymbol.Parameters, s_assemblyEqualityComparer.ParameterEquivalenceComparer))
            {
                rudeEdit = RudeEditKind.ChangingLambdaParameters;
            }
            else if (!s_assemblyEqualityComparer.ReturnTypeEquals(oldLambdaSymbol, newLambdaSymbol))
            {
                rudeEdit = RudeEditKind.ChangingLambdaReturnType;
            }
            else
            {
                hasErrors = false;
                return;
            }

            diagnostics.Add(new RudeEditDiagnostic(
                rudeEdit,
                GetDiagnosticSpan(newLambda, EditKind.Update),
                newLambda,
                new[] { GetDisplayName(newLambda) }));

            hasErrors = true;
        }

        private static ITypeSymbol GetType(ISymbol localOrParameter)
        {
            switch (localOrParameter.Kind)
            {
                case SymbolKind.Parameter:
                    return ((IParameterSymbol)localOrParameter).Type;

                case SymbolKind.Local:
                    return ((ILocalSymbol)localOrParameter).Type;

                default:
                    throw ExceptionUtilities.UnexpectedValue(localOrParameter.Kind);
            }
        }

        private SyntaxNode GetCapturedVariableScope(ISymbol localOrParameter, SyntaxNode memberBody, CancellationToken cancellationToken)
        {
            Debug.Assert(localOrParameter.Kind != SymbolKind.RangeVariable);

            if (localOrParameter.Kind == SymbolKind.Parameter)
            {
                var member = localOrParameter.ContainingSymbol;

                // lambda parameters and C# constructor parameters are lifted to their own scope:
                if ((member as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction || HasParameterClosureScope(member))
                {
                    var result = GetSymbolSyntax(localOrParameter, cancellationToken);
                    Debug.Assert(IsLambda(result));
                    return result;
                }

                return memberBody;
            }

            var node = GetSymbolSyntax(localOrParameter, cancellationToken);
            while (true)
            {
                if (IsClosureScope(node))
                {
                    return node;
                }

                node = node.Parent;
            }
        }

        private bool AreEquivalentClosureScopes(SyntaxNode oldScopeOpt, SyntaxNode newScopeOpt, IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
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
            UpdatedMemberInfo updatedInfo,
            ISymbol oldMember,
            List<RudeEditDiagnostic> diagnostics)
        {
            if (!updatedInfo.OldHasStateMachineSuspensionPoint)
            {
                return;
            }

            // Only methods, local functions and anonymous functions can be async/iterators machines, 
            // but don't assume so to be resiliant against errors in code.
            if (!(oldMember is IMethodSymbol oldMethod))
            {
                return;
            }

            var stateMachineAttributeQualifiedName = oldMethod.IsAsync ?
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" :
                "System.Runtime.CompilerServices.IteratorStateMachineAttribute";

            // We assume that the attributes, if exist, are well formed.
            // If not an error will be reported during EnC delta emit.
            if (oldCompilation.GetTypeByMetadataName(stateMachineAttributeQualifiedName) == null)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.UpdatingStateMachineMethodMissingAttribute,
                    GetBodyDiagnosticSpan(updatedInfo.NewBody, EditKind.Update),
                    updatedInfo.NewBody,
                    new[] { stateMachineAttributeQualifiedName }));
            }
        }

        #endregion

        #endregion

        #region Helpers 

        private static SyntaxNode TryGetNode(SyntaxNode root, int position)
        {
            return root.FullSpan.Contains(position) ? root.FindToken(position).Parent : null;
        }

        private static bool TryGetTextSpan(TextLineCollection lines, LinePositionSpan lineSpan, out TextSpan span)
        {
            if (lineSpan.Start.Line >= lines.Count || lineSpan.End.Line >= lines.Count)
            {
                span = default;
                return false;
            }

            var start = lines[lineSpan.Start.Line].Start + lineSpan.Start.Character;
            var end = lines[lineSpan.End.Line].Start + lineSpan.End.Character;
            span = TextSpan.FromBounds(start, end);
            return true;
        }

        #endregion

        #region Testing

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly AbstractEditAndContinueAnalyzer _abstractEditAndContinueAnalyzer;

            public TestAccessor(AbstractEditAndContinueAnalyzer abstractEditAndContinueAnalyzer)
            {
                _abstractEditAndContinueAnalyzer = abstractEditAndContinueAnalyzer;
            }

            internal void AnalyzeSyntax(
                EditScript<SyntaxNode> script,
                Dictionary<SyntaxNode, EditKind> editMap,
                SourceText oldText,
                SourceText newText,
                DocumentId documentId,
                IActiveStatementTrackingService trackingServiceOpt,
                ImmutableArray<ActiveStatement> oldActiveStatements,
                [Out] ActiveStatement[] newActiveStatements,
                [Out] ImmutableArray<LinePositionSpan>[] newExceptionRegions,
                [Out] List<UpdatedMemberInfo> updatedMethods,
                [Out] List<RudeEditDiagnostic> diagnostics)
            {
                _abstractEditAndContinueAnalyzer.AnalyzeSyntax(script, editMap, oldText, newText, documentId, trackingServiceOpt, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedMethods, diagnostics);
            }

            internal void AnalyzeUnchangedDocument(
                ImmutableArray<ActiveStatement> oldActiveStatements,
                SourceText newText,
                SyntaxNode newRoot,
                DocumentId documentId,
                IActiveStatementTrackingService trackingServiceOpt,
                [In, Out] ActiveStatement[] newActiveStatements,
                [In, Out] ImmutableArray<LinePositionSpan>[] newExceptionRegionsOpt)
            {
                _abstractEditAndContinueAnalyzer.AnalyzeUnchangedDocument(oldActiveStatements, newText, newRoot, documentId, trackingServiceOpt, newActiveStatements, newExceptionRegionsOpt);
            }

            internal BidirectionalMap<SyntaxNode> ComputeMap(
                Match<SyntaxNode> bodyMatch,
                ActiveNode[] activeNodes,
                ref Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas,
                List<RudeEditDiagnostic> diagnostics)
            {
                return _abstractEditAndContinueAnalyzer.ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics);
            }

            internal Match<SyntaxNode> ComputeBodyMatch(
                SyntaxNode oldBody,
                SyntaxNode newBody,
                ActiveNode[] activeNodes,
                List<RudeEditDiagnostic> diagnostics,
                out bool oldHasStateMachineSuspensionPoint,
                out bool newHasStateMachineSuspensionPoint)
            {
                return _abstractEditAndContinueAnalyzer.ComputeBodyMatch(oldBody, newBody, activeNodes, diagnostics, out oldHasStateMachineSuspensionPoint, out newHasStateMachineSuspensionPoint);
            }

            internal void AnalyzeTrivia(
                SourceText oldSource,
                SourceText newSource,
                Match<SyntaxNode> topMatch,
                Dictionary<SyntaxNode, EditKind> editMap,
                [Out] List<(SyntaxNode OldNode, SyntaxNode NewNode)> triviaEdits,
                [Out] List<LineChange> lineEdits,
                [Out] List<RudeEditDiagnostic> diagnostics,
                CancellationToken cancellationToken)
            {
                _abstractEditAndContinueAnalyzer.AnalyzeTrivia(oldSource, newSource, topMatch, editMap, triviaEdits, lineEdits, diagnostics, cancellationToken);
            }

            internal void AnalyzeSemantics(
                EditScript<SyntaxNode> editScript,
                Dictionary<SyntaxNode, EditKind> editMap,
                SourceText oldText,
                ImmutableArray<ActiveStatement> oldActiveStatements,
                List<(SyntaxNode OldNode, SyntaxNode NewNode)> triviaEdits,
                List<UpdatedMemberInfo> updatedMembers,
                SemanticModel oldModel,
                SemanticModel newModel,
                [Out] List<SemanticEdit> semanticEdits,
                [Out] List<RudeEditDiagnostic> diagnostics,
                out Diagnostic firstDeclarationErrorOpt,
                CancellationToken cancellationToken)
            {
                _abstractEditAndContinueAnalyzer.AnalyzeSemantics(editScript, editMap, oldText, oldActiveStatements, triviaEdits, updatedMembers, oldModel, newModel, semanticEdits, diagnostics, out firstDeclarationErrorOpt, cancellationToken);
            }
        }

        #endregion
    }
}
