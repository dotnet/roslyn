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
        /// Finds a statement at given position and a declaration body.
        /// Also returns the corresponding partner statement in <paramref name="partnerDeclarationBodyOpt"/>, if specified.
        /// </summary>
        /// <remarks>
        /// The declaration body node may not contain the <paramref name="position"/>. 
        /// This happens when an active statement associated with the member is outside of its body (e.g. C# constructor).
        /// </remarks>
        protected abstract SyntaxNode FindStatementAndPartner(SyntaxNode declarationBody, int position, SyntaxNode partnerDeclarationBodyOpt, out SyntaxNode partnerOpt, out int statementPart);

        private SyntaxNode FindStatement(SyntaxNode declarationBody, int position, out int statementPart)
        {
            SyntaxNode partner;
            return FindStatementAndPartner(declarationBody, position, null, out partner, out statementPart);
        }

        /// <summary>
        /// Returns a function that maps nodes of <paramref name="newRoot"/> to corresponding nodes of <paramref name="oldRoot"/>,
        /// assuming that the bodies only differ in trivia.
        /// </summary>
        internal abstract Func<SyntaxNode, SyntaxNode> CreateSyntaxMapForEquivalentNodes(SyntaxNode oldRoot, SyntaxNode newRoot);

        /// <summary>
        /// Returns a node that represents a body of a lambda containing specified <paramref name="node"/>,
        /// or null if the node isn't contained in a lambda. If a node is returned it must uniquely represent the lambda,
        /// i.e. be no two distinct nodes may represent the same lambda.
        /// </summary>
        protected abstract SyntaxNode FindEnclosingLambdaBody(SyntaxNode containerOpt, SyntaxNode node);

        protected abstract SyntaxNode GetPartnerLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda);

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
        protected abstract bool TryGetActiveSpan(SyntaxNode node, int statementPart, out TextSpan span);

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
        /// Returns true if the code emitted for the old active statement part (<paramref name="statementPart"/> of <paramref name="oldStatement"/>) 
        /// is the same as the code emitted for the corresponding new active statement part (<paramref name="statementPart"/> of <paramref name="newStatement"/>). 
        /// </summary>
        /// <remarks>
        /// A rude edit is reported if an active statement is changed and this method returns true.
        /// </remarks>
        protected abstract bool AreEquivalentActiveStatements(SyntaxNode oldStatement, SyntaxNode newStatement, int statementPart);

        protected abstract ISymbol GetSymbolForEdit(SemanticModel model, SyntaxNode node, EditKind editKind, Dictionary<SyntaxNode, EditKind> editMap, CancellationToken cancellationToken);

        protected abstract TextSpan GetDiagnosticSpan(SyntaxNode node, EditKind editKind);
        protected abstract string GetTopLevelDisplayName(SyntaxNode node, EditKind editKind);
        protected abstract string GetStatementDisplayName(SyntaxNode node, EditKind editKind);
        protected abstract string GetLambdaDisplayName(SyntaxNode lambda);
        protected abstract List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, bool isLeaf);
        protected abstract ImmutableArray<SyntaxNode> GetStateMachineSuspensionPoints(SyntaxNode body);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportSyntacticRudeEdits(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(List<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, SyntaxNode newStatement);
        internal abstract void ReportOtherRudeEditsAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode oldStatement, SyntaxNode newStatement, bool isLeaf);
        internal abstract void ReportMemberUpdateRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode newMember, TextSpan? span);
        internal abstract void ReportInsertedMemberSymbolRudeEdits(List<RudeEditDiagnostic> diagnostics, ISymbol newSymbol);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract bool IsMethod(SyntaxNode declaration);
        internal abstract bool IsLambda(SyntaxNode node);
        internal abstract bool ContainsLambda(SyntaxNode declaration);

        /// <summary>
        /// Returns all lambda bodies of a node representing a lambda.
        /// </summary>
        /// <remarks>
        /// C# anonymous function expression and VB lambda expression both have a single body
        /// (in VB the body is the header of the lambda expression).
        /// 
        /// Some lambda queries (group by, join by) have two bodies.
        /// </remarks>
        internal abstract bool TryGetLambdaBodies(SyntaxNode node, out SyntaxNode body1, out SyntaxNode body2);

        internal abstract bool IsStateMachineMethod(SyntaxNode declaration);
        internal abstract SyntaxNode TryGetContainingTypeDeclaration(SyntaxNode memberDeclaration);

        internal abstract bool HasBackingField(SyntaxNode propertyDeclaration);

        /// <summary>
        /// Return true if the declaration is a field/property declaration with an initializer. 
        /// Shall return false for enum members.
        /// </summary>
        internal abstract bool HasInitializer(SyntaxNode declaration, out bool isStatic);

        private bool HasInitializer(SyntaxNode declaration)
        {
            bool isStatic;
            return HasInitializer(declaration, out isStatic);
        }

        internal abstract bool IncludesInitializers(SyntaxNode constructorDeclaration);
        internal abstract bool IsPartial(INamedTypeSymbol type);
        internal abstract SyntaxNode EmptyCompilationUnit { get; }

        private static readonly SourceText s_emptySource = SourceText.From("");

        #region Document Analysis 

        public async Task<DocumentAnalysisResults> AnalyzeDocumentAsync(
            Solution baseSolution,
            ImmutableArray<ActiveStatementSpan> baseActiveStatements,
            Document document,
            CancellationToken cancellationToken)
        {
            DocumentAnalysisResults.Log.Write("Analyzing document {0}", document.Name);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxTree oldTreeOpt;
                SyntaxNode oldRoot;
                SourceText oldText;

                var oldProject = baseSolution.GetProject(document.Project.Id);
                var oldDocumentOpt = oldProject?.GetDocument(document.Id);

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
                var trackingService = baseSolution.Workspace.Services.GetService<IActiveStatementTrackingService>();

                cancellationToken.ThrowIfCancellationRequested();

                // TODO: newTree.HasErrors?
                var syntaxDiagnostics = newRoot.GetDiagnostics();
                var syntaxErrorCount = syntaxDiagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

                var newActiveStatements = new LinePositionSpan[baseActiveStatements.Length];
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
                        trackingService,
                        newActiveStatements,
                        newExceptionRegions);

                    if (syntaxErrorCount > 0)
                    {
                        DocumentAnalysisResults.Log.Write("{0}: unchanged with syntax errors ({1})", document.Name, syntaxDiagnostics.First().Location);
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
                    DocumentAnalysisResults.Log.Write("{0}: syntax error ({1} total)", syntaxDiagnostics.First().Location, syntaxErrorCount);

                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray<RudeEditDiagnostic>.Empty);
                }

                // Disallow modification of a file with experimental features enabled.
                // These features may not be handled well by the analysis below.
                if (ExperimentalFeaturesEnabled(newTree))
                {
                    DocumentAnalysisResults.Log.Write("{0}: experimental features enabled", document.Name);

                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.ExperimentalFeaturesEnabled, default(TextSpan))));
                }

                // We do calculate diffs even if there are semantic errors for the following reasons: 
                // 1) We need to be able to find active spans in the new document. 
                //    If we didn't calculate them we would only rely on tracking spans (might be ok).
                // 2) If there are syntactic rude edits we'll report them faster without waiting for semantic analysis.
                //    The user may fix them before they address all the semantic errors.

                // [(edit ordinal, matching statements of an updated active method)]
                var updatedActiveMethodMatches = new List<ValueTuple<int, IReadOnlyDictionary<SyntaxNode, SyntaxNode>>>();

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
                    trackingService,
                    baseActiveStatements,
                    newActiveStatements,
                    newExceptionRegions,
                    updatedActiveMethodMatches,
                    diagnostics);

                if (diagnostics.Count > 0)
                {
                    DocumentAnalysisResults.Log.Write("{0} syntactic rude edits, first: {1}{2}", diagnostics.Count, document.FilePath, diagnostics.First().Span);
                    return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), diagnostics.AsImmutable());
                }

                // Disallow addition of a new file.
                // During EnC, a new file cannot be added to the current solution, but some IDE features (i.e., CodeFix) try to do so. 
                // In most cases, syntactic rude edits detect them with specific reasons but some reach up to here and we bail them out with a general message.
                if (oldDocumentOpt == null)
                {
                    DocumentAnalysisResults.Log.Write("A new file added: {0}", document.Name);
                    return DocumentAnalysisResults.SyntaxErrors(ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.RUDE_EDIT_ADD_NEW_FILE, default(TextSpan))));
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Bail if there were any semantic errors in the compilation.
                var newCompilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var firstError = newCompilation.GetDiagnostics(cancellationToken).FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
                if (firstError != null)
                {
                    DocumentAnalysisResults.Log.Write("Semantic errors, first: {0}", firstError.Location);

                    return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), ImmutableArray.Create<RudeEditDiagnostic>(), hasSemanticErrors: true);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var triviaEdits = new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
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
                    DocumentAnalysisResults.Log.Write("{0} trivia rude edits, first: {1}{2}", diagnostics.Count, document.FilePath, diagnostics.First().Span);
                    return DocumentAnalysisResults.Errors(newActiveStatements.AsImmutable(), diagnostics.AsImmutable());
                }

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
                        updatedActiveMethodMatches,
                        oldModel,
                        newModel,
                        semanticEdits,
                        diagnostics,
                        cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (diagnostics.Count > 0)
                    {
                        DocumentAnalysisResults.Log.Write("{0}{1}: semantic rude edit ({2} total)", document.FilePath, diagnostics.First().Span, diagnostics.Count);
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
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
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

        internal void AnalyzeSyntax(
            EditScript<SyntaxNode> script,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingService,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            [Out]LinePositionSpan[] newActiveStatements,
            [Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [Out]List<ValueTuple<int, IReadOnlyDictionary<SyntaxNode, SyntaxNode>>> updatedActiveMethodMatches,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);
            Debug.Assert(updatedActiveMethodMatches != null);
            Debug.Assert(updatedActiveMethodMatches.Count == 0);

            var updatedTrackingSpans = new List<KeyValuePair<ActiveStatementId, TextSpan>>();

            for (int i = 0; i < script.Edits.Length; i++)
            {
                var edit = script.Edits[i];

                AnalyzeUpdatedActiveMethodBodies(script, i, editMap, oldText, newText, documentId, trackingService, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedActiveMethodMatches, updatedTrackingSpans, diagnostics);
                ReportSyntacticRudeEdits(diagnostics, script.Match, edit, editMap);
            }

            UpdateUneditedSpans(diagnostics, script.Match, oldText, newText, documentId, trackingService, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedTrackingSpans);

            if (updatedTrackingSpans.Count > 0)
            {
                trackingService.UpdateActiveStatementSpans(newText, updatedTrackingSpans);
            }
        }

        private void UpdateUneditedSpans(
            List<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> topMatch,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingService,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            [In, Out]LinePositionSpan[] newActiveStatements,
            [In, Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [In, Out]List<KeyValuePair<ActiveStatementId, TextSpan>> updatedTrackingSpans)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);

            // Active statements in methods that were not updated 
            // are not changed but their spans might have been. 

            for (int i = 0; i < newActiveStatements.Length; i++)
            {
                if (newActiveStatements[i] == default(LinePositionSpan))
                {
                    Debug.Assert(newExceptionRegions[i].IsDefault);

                    TextSpan trackedSpan = default(TextSpan);
                    bool isTracked = trackingService != null &&
                                     trackingService.TryGetSpan(new ActiveStatementId(documentId, i), newText, out trackedSpan);

                    TextSpan oldStatementSpan;
                    if (!TryGetTextSpan(oldText.Lines, oldActiveStatements[i].Span, out oldStatementSpan))
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: {0}", oldStatementSpan);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    SyntaxNode oldMember = FindMemberDeclaration(topMatch.OldRoot, oldStatementSpan.Start);

                    // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                    if (oldMember == null)
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: {0}", oldStatementSpan);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    SyntaxNode newMember;
                    bool hasPartner = topMatch.TryGetNewNode(oldMember, out newMember);
                    Debug.Assert(hasPartner);

                    SyntaxNode oldBody = TryGetDeclarationBody(oldMember, isMember: true);
                    SyntaxNode newBody = TryGetDeclarationBody(newMember, isMember: true);

                    // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                    if (oldBody == null || newBody == null)
                    {
                        DocumentAnalysisResults.Log.Write("Invalid active statement span: {0}", oldStatementSpan);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                        continue;
                    }

                    int statementPart = -1;
                    SyntaxNode newStatement = null;

                    // The tracking span might have been deleted or moved outside of the member span.
                    // It is not an error to move the statement - we just ignore it.
                    if (isTracked && trackedSpan.Length != 0 && newMember.Span.Contains(trackedSpan))
                    {
                        int trackedStatementPart;
                        var trackedStatement = FindStatement(newBody, trackedSpan.Start, out trackedStatementPart);

                        // In rare cases the tracking span might have been moved outside of lambda.
                        // It is not an error to move the statement - we just ignore it.
                        var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody, oldMember.FindToken(oldStatementSpan.Start).Parent);
                        var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody, trackedStatement);
                        if (oldEnclosingLambdaBody == newEnclosingLambdaBody)
                        {
                            newStatement = trackedStatement;
                            statementPart = trackedStatementPart;
                        }
                    }

                    if (newStatement == null)
                    {
                        Debug.Assert(statementPart == -1);
                        FindStatementAndPartner(oldBody, oldStatementSpan.Start, newBody, out newStatement, out statementPart);
                    }

                    if (diagnostics.Count == 0)
                    {
                        List<SyntaxNode> ancestors = GetExceptionHandlingAncestors(newStatement, oldActiveStatements[i].IsLeaf);
                        newExceptionRegions[i] = GetExceptionRegions(ancestors, newText);
                    }

                    // Even though the body of the declaration haven't changed, 
                    // changes to its header might have caused the active span to become unavailable.
                    // (e.g. In C# "const" was added to modifiers of a field with an initializer).
                    TextSpan newStatementSpan = FindClosestActiveSpan(newStatement, statementPart);

                    newActiveStatements[i] = newText.Lines.GetLinePositionSpan(newStatementSpan);

                    // Update tracking span if we found a matching active statement whose span is different.
                    if (isTracked && newStatementSpan != trackedSpan)
                    {
                        updatedTrackingSpans.Add(KeyValuePair.Create(new ActiveStatementId(documentId, i), newStatementSpan));
                    }
                }
            }
        }

        // internal for testing
        internal void AnalyzeUnchangedDocument(
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            SourceText newText,
            SyntaxNode newRoot,
            DocumentId documentId,
            IActiveStatementTrackingService trackingService,
            [In, Out]LinePositionSpan[] newActiveStatements,
            [In, Out]ImmutableArray<LinePositionSpan>[] newExceptionRegionsOpt)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(newExceptionRegionsOpt == null || oldActiveStatements.Length == newExceptionRegionsOpt.Length);

            var updatedTrackingSpans = new List<KeyValuePair<ActiveStatementId, TextSpan>>();

            // Active statements in methods that were not updated 
            // are not changed but their spans might have been. 

            for (int i = 0; i < newActiveStatements.Length; i++)
            {
                TextSpan oldStatementSpan, newStatementSpan;
                if (!TryGetTextSpan(newText.Lines, oldActiveStatements[i].Span, out oldStatementSpan) ||
                    !TryGetEnclosingBreakpointSpan(newRoot, oldStatementSpan.Start, out newStatementSpan))
                {
                    newExceptionRegionsOpt[i] = ImmutableArray<LinePositionSpan>.Empty;
                    continue;
                }

                var newNode = TryGetNode(newRoot, oldStatementSpan.Start);
                Debug.Assert(newNode != null); // we wouldn't find a breakpoint span otherwise

                if (newExceptionRegionsOpt != null)
                {
                    List<SyntaxNode> ancestors = GetExceptionHandlingAncestors(newNode, oldActiveStatements[i].IsLeaf);
                    newExceptionRegionsOpt[i] = GetExceptionRegions(ancestors, newText);
                }

                newActiveStatements[i] = newText.Lines.GetLinePositionSpan(newStatementSpan);

                // Update tracking span if we found a matching active statement whose span is different.
                TextSpan trackedSpan = default(TextSpan);
                bool isTracked = trackingService != null &&
                                 trackingService.TryGetSpan(new ActiveStatementId(documentId, i), newText, out trackedSpan);

                if (isTracked && newStatementSpan != trackedSpan)
                {
                    updatedTrackingSpans.Add(KeyValuePair.Create(new ActiveStatementId(documentId, i), newStatementSpan));
                }
            }

            if (updatedTrackingSpans.Count > 0)
            {
                trackingService.UpdateActiveStatementSpans(newText, updatedTrackingSpans);
            }
        }

        // internal for testing
        internal struct ActiveNode
        {
            public readonly SyntaxNode OldNode;
            public readonly SyntaxNode NewTrackedNodeOpt;
            public readonly SyntaxNode EnclosingLambdaBodyOpt;
            public readonly int StatementPart;
            public readonly TextSpan? TrackedSpanOpt;

            public ActiveNode(SyntaxNode oldNode, SyntaxNode enclosingLambdaBodyOpt, int statementPart, TextSpan? trackedSpanOpt, SyntaxNode newTrackedNodeOpt)
            {
                Debug.Assert(oldNode != null);

                this.OldNode = oldNode;
                this.NewTrackedNodeOpt = newTrackedNodeOpt;
                this.EnclosingLambdaBodyOpt = enclosingLambdaBodyOpt;
                this.StatementPart = statementPart;
                this.TrackedSpanOpt = trackedSpanOpt;
            }
        }

        // internal for testing
        internal struct LambdaInfo
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
                this.ActiveNodeIndices = activeNodeIndices;
                this.Match = match;
                this.NewBody = newLambdaBody;
            }

            public LambdaInfo WithMatch(Match<SyntaxNode> match, SyntaxNode newLambdaBody)
            {
                return new LambdaInfo(this.ActiveNodeIndices, match, newLambdaBody);
            }
        }

        private void AnalyzeUpdatedActiveMethodBodies(
            EditScript<SyntaxNode> topEditScript,
            int editOrdinal,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            SourceText newText,
            DocumentId documentId,
            IActiveStatementTrackingService trackingService,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            [Out]LinePositionSpan[] newActiveStatements,
            [Out]ImmutableArray<LinePositionSpan>[] newExceptionRegions,
            [Out]List<ValueTuple<int, IReadOnlyDictionary<SyntaxNode, SyntaxNode>>> updatedActiveMethodMatches,
            [Out]List<KeyValuePair<ActiveStatementId, TextSpan>> updatedTrackingSpans,
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

            SyntaxNode oldBody, newBody;
            if (!TryGetDeclarationBodyEdit(edit, editMap, out oldBody, out newBody) || oldBody == null)
            {
                return;
            }

            // We need to process edited methods with active statements and async/iterator methods (regardless if they contain an active statement).
            // Async/iterator methods are considered always active since we don't know when the state machine is in a suspended state
            // from looking at active statements.
            int start, end;
            bool hasActiveStatement = TryGetOverlappingActiveStatements(oldText, edit.OldNode.Span, oldActiveStatements, out start, out end);

            if (edit.Kind == EditKind.Delete)
            {
                // The entire member has been deleted.

                // TODO: if the member isn't a field/property we should return empty span.
                // We need to adjust the tracking span design and UpdateUneditedSpans to account for such empty spans.
                if (hasActiveStatement)
                {
                    var newSpan = HasInitializer(edit.OldNode) ?
                        GetDeletedNodeActiveSpan(topEditScript.Match, edit.OldNode) :
                        GetDeletedNodeDiagnosticSpan(topEditScript.Match, edit.OldNode);

                    for (int i = start; i < end; i++)
                    {
                        // TODO: VB field multi-initializers break this
                        // Debug.Assert(newActiveStatements[i] == default(LinePositionSpan));

                        Debug.Assert(newSpan != default(TextSpan));
                        newActiveStatements[i] = newText.Lines.GetLinePositionSpan(newSpan);
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
                    for (int i = start; i < end; i++)
                    {
                        Debug.Assert(newActiveStatements[i] == default(LinePositionSpan) && newSpan != default(TextSpan));
                        newActiveStatements[i] = newText.Lines.GetLinePositionSpan(newSpan);
                        newExceptionRegions[i] = ImmutableArray.Create<LinePositionSpan>();
                    }
                }

                return;
            }

            // Populated with active lambdas and matched lambdas. 
            // Unmatched non-active lambdas are not included.
            Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas = null;

            // finds leaf nodes that correspond to the old active statements:
            Debug.Assert(end > start || !hasActiveStatement && end == start);
            var activeNodes = new ActiveNode[end - start];
            for (int i = 0; i < activeNodes.Length; i++)
            {
                int ordinal = start + i;
                int statementPart;

                var oldStatementStart = oldText.Lines.GetTextSpan(oldActiveStatements[ordinal].Span).Start;
                var oldStatementSyntax = FindStatement(oldBody, oldStatementStart, out statementPart);
                var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody, oldStatementSyntax);

                if (oldEnclosingLambdaBody != null)
                {
                    if (lazyActiveOrMatchedLambdas == null)
                    {
                        lazyActiveOrMatchedLambdas = new Dictionary<SyntaxNode, LambdaInfo>();
                    }

                    LambdaInfo lambda;
                    if (!lazyActiveOrMatchedLambdas.TryGetValue(oldEnclosingLambdaBody, out lambda))
                    {
                        lambda = new LambdaInfo(new List<int>());
                        lazyActiveOrMatchedLambdas.Add(oldEnclosingLambdaBody, lambda);
                    }

                    lambda.ActiveNodeIndices.Add(i);
                }

                // Tracking spans corresponding to the active statements from the tracking service.
                // We seed the method body matching algorithm with tracking spans (unless they were deleted)
                // to get precise matching.
                TextSpan trackedSpan = default(TextSpan);
                SyntaxNode trackedNode = null;
                bool isTracked = trackingService?.TryGetSpan(new ActiveStatementId(documentId, ordinal), newText, out trackedSpan) ?? false;

                if (isTracked)
                {
                    // The tracking span might have been deleted or moved outside of the member span.
                    // It is not an error to move the statement - we just ignore it.
                    if (trackedSpan.Length != 0 && edit.NewNode.Span.Contains(trackedSpan))
                    {
                        int part;
                        var newStatementSyntax = FindStatement(newBody, trackedSpan.Start, out part);
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

            bool hasStateMachineSuspensionPoint;
            bool hasLambda;
            var bodyMatch = ComputeBodyMatch(oldBody, newBody, activeNodes.Where(n => n.EnclosingLambdaBodyOpt == null).ToArray(), diagnostics, out hasStateMachineSuspensionPoint);
            var reverseMap = ComputeReverseMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics, out hasLambda);

            // TODO: include reverse maps of field initializers
            if (IsMethod(edit.OldNode) && (hasStateMachineSuspensionPoint || hasLambda || hasActiveStatement))
            {
                // Save the body match for local variable mapping.
                // We'll use it to tell the compiler what local variables to preserve in an active method.
                // An edited async/iterator method is considered active.
                updatedActiveMethodMatches.Add(ValueTuple.Create(editOrdinal, reverseMap));
            }

            for (int i = 0; i < activeNodes.Length; i++)
            {
                int ordinal = start + i;
                bool hasMatching = false;
                bool isLeaf = (oldActiveStatements[ordinal].Flags & ActiveStatementFlags.LeafFrame) != 0;
                int statementPart = activeNodes[i].StatementPart;
                var oldStatementSyntax = activeNodes[i].OldNode;
                var oldEnclosingLambdaBody = activeNodes[i].EnclosingLambdaBodyOpt;

                newExceptionRegions[ordinal] = ImmutableArray.Create<LinePositionSpan>();

                TextSpan newSpan;
                SyntaxNode newStatementSyntax;
                Match<SyntaxNode> match;

                if (oldEnclosingLambdaBody == null)
                {
                    match = bodyMatch;

                    hasMatching = TryMatchActiveStatement(oldStatementSyntax, statementPart, oldBody, newBody, out newStatementSyntax) ||
                                  match.TryGetNewNode(oldStatementSyntax, out newStatementSyntax);
                }
                else
                {
                    var oldLambdaInfo = lazyActiveOrMatchedLambdas[oldEnclosingLambdaBody];
                    SyntaxNode newEnclosingLambdaBody = oldLambdaInfo.NewBody;
                    match = oldLambdaInfo.Match;

                    if (match != null)
                    {
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
                    Debug.Assert(newStatementSyntax != null);

                    // The matching node doesn't produce sequence points.
                    // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
                    newSpan = FindClosestActiveSpan(newStatementSyntax, statementPart);

                    if (!isLeaf && !AreEquivalentActiveStatements(oldStatementSyntax, newStatementSyntax, statementPart))
                    {
                        // rude edit: internal active statement changed
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ActiveStatementUpdate, newSpan));
                    }

                    // exception handling around the statement:

                    var oldAncestors = GetExceptionHandlingAncestors(oldStatementSyntax, isLeaf);
                    var newAncestors = GetExceptionHandlingAncestors(newStatementSyntax, isLeaf);

                    if (oldAncestors.Count > 0 || newAncestors.Count > 0)
                    {
                        var edits = match.GetSequenceEdits(oldAncestors, newAncestors);
                        ReportEnclosingExceptionHandlingRudeEdits(diagnostics, edits, oldStatementSyntax, newStatementSyntax);

                        // Exception regions are not needed in presence of errors.
                        if (diagnostics.Count == 0)
                        {
                            Debug.Assert(oldAncestors.Count == newAncestors.Count);
                            newExceptionRegions[ordinal] = GetExceptionRegions(newAncestors, newText);
                        }
                    }

                    // other statements around active statement:
                    ReportOtherRudeEditsAroundActiveStatement(diagnostics, match, oldStatementSyntax, newStatementSyntax, isLeaf);
                }
                else if (match == null)
                {
                    Debug.Assert(oldEnclosingLambdaBody != null);

                    newSpan = GetDeletedNodeDiagnosticSpan(oldEnclosingLambdaBody, bodyMatch, lazyActiveOrMatchedLambdas);

                    // Lambda containing the active statement can't be found in the new source.
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ActiveStatementLambdaRemoved, newSpan, oldEnclosingLambdaBody.Parent,
                        new[] { GetLambdaDisplayName(oldEnclosingLambdaBody.Parent) }));
                }
                else
                {
                    newSpan = GetDeletedNodeActiveSpan(match, oldStatementSyntax);

                    if (!isLeaf)
                    {
                        // rude edit: internal active statement deleted
                        diagnostics.Add(
                            new RudeEditDiagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED,
                            GetDeletedNodeDiagnosticSpan(match, oldStatementSyntax)));
                    }
                }

                Debug.Assert(newActiveStatements[ordinal] == default(LinePositionSpan) && newSpan != default(TextSpan));
                newActiveStatements[ordinal] = newText.Lines.GetLinePositionSpan(newSpan);

                // Update tracking span if we found a matching active statement whose span is different.
                // It could have been deleted or moved out of the method/lambda body, in which case we set it to empty.
                if (activeNodes[i].TrackedSpanOpt.HasValue && activeNodes[i].TrackedSpanOpt.Value != newSpan)
                {
                    updatedTrackingSpans.Add(KeyValuePair.Create(new ActiveStatementId(documentId, ordinal), newSpan));
                }
            }
        }

        /// <summary>
        /// Calculates a syntax map of the entire method body including all lambda bodies it contains (recursively).
        /// Internal for testing.
        /// </summary>
        internal IReadOnlyDictionary<SyntaxNode, SyntaxNode> ComputeReverseMap(
            Match<SyntaxNode> bodyMatch,
            ActiveNode[] activeNodes,
            ref Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas,
            List<RudeEditDiagnostic> diagnostics,
            out bool hasLambda)
        {
            ArrayBuilder<Match<SyntaxNode>> lambdaBodyMatches = null;
            int currentLambdaBodyMatch = -1;
            Match<SyntaxNode> currentBodyMatch = bodyMatch;

            while (true)
            {
                foreach (var pair in currentBodyMatch.Matches)
                {
                    // Skip root, only enumerate body matches.
                    if (pair.Key == currentBodyMatch.OldRoot)
                    {
                        Debug.Assert(pair.Value == currentBodyMatch.NewRoot);
                        continue;
                    }

                    SyntaxNode oldLambda = pair.Key;
                    SyntaxNode newLambda = pair.Value;
                    SyntaxNode oldLambdaBody1, oldLambdaBody2;

                    if (IsLambda(oldLambda))
                    {
                        Debug.Assert(IsLambda(newLambda));

                        if (TryGetLambdaBodies(oldLambda, out oldLambdaBody1, out oldLambdaBody2))
                        {
                            if (lambdaBodyMatches == null)
                            {
                                lambdaBodyMatches = ArrayBuilder<Match<SyntaxNode>>.GetInstance();
                            }

                            if (lazyActiveOrMatchedLambdas == null)
                            {
                                lazyActiveOrMatchedLambdas = new Dictionary<SyntaxNode, LambdaInfo>();
                            }

                            lambdaBodyMatches.Add(ComputeLambdaBodyMatch(oldLambdaBody1, newLambda, activeNodes, lazyActiveOrMatchedLambdas, diagnostics));

                            if (oldLambdaBody2 != null)
                            {
                                lambdaBodyMatches.Add(ComputeLambdaBodyMatch(oldLambdaBody2, newLambda, activeNodes, lazyActiveOrMatchedLambdas, diagnostics));
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
                hasLambda = false;
                return bodyMatch.ReverseMatches;
            }

            var result = new Dictionary<SyntaxNode, SyntaxNode>();

            // include all matches, including the root:
            result.AddRange(bodyMatch.ReverseMatches);

            foreach (var lambdaBodyMatch in lambdaBodyMatches)
            {
                foreach (var pair in lambdaBodyMatch.Matches)
                {
                    // Body match of a lambda whose body is an expression has the lambda as a root.
                    // The lambda has already been included when enumerating parent body matches.
                    Debug.Assert(
                        !result.ContainsKey(pair.Value) ||
                        pair.Key == lambdaBodyMatch.OldRoot && pair.Value == lambdaBodyMatch.NewRoot && IsLambda(pair.Key));

                    // reverse
                    result[pair.Value] = pair.Key;
                }
            }

            lambdaBodyMatches?.Free();

            hasLambda = true;
            return result;
        }

        private Match<SyntaxNode> ComputeLambdaBodyMatch(
            SyntaxNode lambdaBody,
            SyntaxNode newLambda,
            ActiveNode[] activeNodes,
            [Out]Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            SyntaxNode newLambdaBody = GetPartnerLambdaBody(lambdaBody, newLambda);

            ActiveNode[] activeNodesInLambda;
            LambdaInfo info;
            if (lazyActiveOrMatchedLambdas.TryGetValue(lambdaBody, out info))
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

            bool needsSyntaxMap;
            var lambdaBodyMatch = ComputeBodyMatch(lambdaBody, newLambdaBody, activeNodesInLambda ?? SpecializedCollections.EmptyArray<ActiveNode>(), diagnostics, out needsSyntaxMap);

            lazyActiveOrMatchedLambdas[lambdaBody] = info.WithMatch(lambdaBodyMatch, newLambdaBody);

            return lambdaBodyMatch;
        }

        // internal for testing
        internal Match<SyntaxNode> ComputeBodyMatch(
            SyntaxNode oldBody,
            SyntaxNode newBody,
            ActiveNode[] activeNodes,
            List<RudeEditDiagnostic> diagnostics,
            out bool hasStateMachineSuspensionPoint)
        {
            Debug.Assert(oldBody != null);
            Debug.Assert(newBody != null);
            Debug.Assert(activeNodes != null);
            Debug.Assert(diagnostics != null);

            List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches = null;
            List<SequenceEdit> lazyRudeEdits = null;

            var oldStateMachineSuspensionPoints = GetStateMachineSuspensionPoints(oldBody);
            var newStateMachineSuspensionPoints = GetStateMachineSuspensionPoints(newBody);

            AddMatchingActiveNodes(ref lazyKnownMatches, activeNodes);

            // Consider following cases:
            // 1) Both old and new methods contain yields/awaits.
            //    Map the old suspension points to new ones, report errors for added/deleted suspension points.
            // 2) The old method contains yields/awaits but the new doesn't.
            //    Report rude edits for each deleted yield/await.
            // 3) The new method contains yields/awaits but the old doesn't.
            //    a) If the method has active statements report rude edits for each inserted yield/await (insert "around" an active statement).
            //    b) If the method has no active statements then the edit is valid, we don't need to calculate map.

            bool creatingStateMachineAroundActiveStatement = oldStateMachineSuspensionPoints.Length == 0 && newStateMachineSuspensionPoints.Length > 0 && activeNodes.Length > 0;
            hasStateMachineSuspensionPoint = oldStateMachineSuspensionPoints.Length > 0 && newStateMachineSuspensionPoints.Length > 0;

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

                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.Delete,
                            GetDeletedNodeDiagnosticSpan(match, deletedNode),
                            deletedNode,
                            new[] { GetStatementDisplayName(deletedNode, EditKind.Delete) }));
                    }
                    else
                    {
                        Debug.Assert(rudeEdit.Kind == EditKind.Insert);

                        var insertedNode = newStateMachineSuspensionPoints[rudeEdit.NewIndex];

                        diagnostics.Add(new RudeEditDiagnostic(
                            creatingStateMachineAroundActiveStatement ? RudeEditKind.RUDE_EDIT_INSERT_AROUND : RudeEditKind.Insert,
                            GetDiagnosticSpan(insertedNode, EditKind.Insert),
                            insertedNode,
                            new[] { GetStatementDisplayName(insertedNode, EditKind.Insert) }));
                    }
                }
            }
            else if (oldStateMachineSuspensionPoints.Length > 0)
            {
                Debug.Assert(oldStateMachineSuspensionPoints.Length == newStateMachineSuspensionPoints.Length);

                for (int i = 0; i < oldStateMachineSuspensionPoints.Length; i++)
                {
                    ReportStateMachineSuspensionPointRudeEdits(diagnostics, oldStateMachineSuspensionPoints[i], newStateMachineSuspensionPoints[i]);
                }
            }

            return match;
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

                    lazyKnownMatches.Add(KeyValuePair.Create(activeNode.OldNode, activeNode.NewTrackedNodeOpt));
                }
            }
        }

        private void AddMatchingStateMachineSuspensionPoints(
            ref List<KeyValuePair<SyntaxNode, SyntaxNode>> lazyKnownMatches,
            ref List<SequenceEdit> lazyRudeEdits,
            ImmutableArray<SyntaxNode> oldStateMachineSuspensionPoints,
            ImmutableArray<SyntaxNode> newStateMachineSuspensionPoints)
        {
            // State machine suspension points (yield statements and await expressions) determine the structure of the generated state machine.
            // Change of the SM structure is far more significant then changes of the value (arguments) of these nodes.
            // Hence we build the match such that these nodes are fixed.

            if (lazyKnownMatches == null)
            {
                lazyKnownMatches = new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
            }

            if (oldStateMachineSuspensionPoints.Length == newStateMachineSuspensionPoints.Length)
            {
                for (int i = 0; i < oldStateMachineSuspensionPoints.Length; i++)
                {
                    lazyKnownMatches.Add(KeyValuePair.Create(oldStateMachineSuspensionPoints[i], newStateMachineSuspensionPoints[i]));
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
                        lazyKnownMatches.Add(KeyValuePair.Create(oldStateMachineSuspensionPoints[edit.OldIndex], newStateMachineSuspensionPoints[edit.NewIndex]));
                    }
                    else
                    {
                        if (lazyRudeEdits == null)
                        {
                            lazyRudeEdits = new List<SequenceEdit>();
                        }

                        lazyRudeEdits.Add(edit);
                    }
                }

                Debug.Assert(lazyRudeEdits != null);
            }
        }

        ImmutableArray<LinePositionSpan> IEditAndContinueAnalyzer.GetExceptionRegions(SourceText text, SyntaxNode syntaxRoot, LinePositionSpan activeStatementSpan, bool isLeaf)
        {
            return GetExceptionRegions(text, syntaxRoot, activeStatementSpan, isLeaf);
        }

        internal ImmutableArray<LinePositionSpan> GetExceptionRegions(SourceText text, SyntaxNode syntaxRoot, LinePositionSpan activeStatementSpan, bool isLeaf)
        {
            var textSpan = text.Lines.GetTextSpan(activeStatementSpan);
            var token = syntaxRoot.FindToken(textSpan.Start);
            var ancestors = GetExceptionHandlingAncestors(token.Parent, isLeaf);
            return GetExceptionRegions(ancestors, text);
        }

        private ImmutableArray<LinePositionSpan> GetExceptionRegions(List<SyntaxNode> exceptionHandlingAncestors, SourceText text)
        {
            if (exceptionHandlingAncestors.Count == 0)
            {
                return ImmutableArray.Create<LinePositionSpan>();
            }

            var result = new List<LinePositionSpan>();

            for (int i = exceptionHandlingAncestors.Count - 1; i >= 0; i--)
            {
                bool coversAllChildren;
                TextSpan span = GetExceptionHandlingRegion(exceptionHandlingAncestors[i], out coversAllChildren);

                result.Add(text.Lines.GetLinePositionSpan(span));

                // Exception regions describe regions of code that can't be edited.
                // If the span covers all of the nodes children we don't need to descend further.
                if (coversAllChildren)
                {
                    break;
                }
            }

            return result.AsImmutable();
        }

        private TextSpan GetDeletedNodeDiagnosticSpan(SyntaxNode deletedLambdaBody, Match<SyntaxNode> bodyMatch, Dictionary<SyntaxNode, LambdaInfo> lambdaInfos)
        {
            SyntaxNode oldLambdaBody = deletedLambdaBody;
            while (true)
            {
                var oldParentLambdaBody = FindEnclosingLambdaBody(bodyMatch.OldRoot, oldLambdaBody.Parent);
                if (oldParentLambdaBody == null)
                {
                    return GetDeletedNodeDiagnosticSpan(bodyMatch, oldLambdaBody);
                }

                LambdaInfo lambdaInfo;
                if (lambdaInfos.TryGetValue(oldParentLambdaBody, out lambdaInfo) && lambdaInfo.Match != null)
                {
                    return GetDeletedNodeDiagnosticSpan(lambdaInfo.Match, oldLambdaBody);
                }

                oldLambdaBody = oldParentLambdaBody;
            }
        }

        private TextSpan FindClosestActiveSpan(SyntaxNode statement, int statementPart)
        {
            TextSpan span;
            if (TryGetActiveSpan(statement, statementPart, out span))
            {
                return span;
            }

            // The node doesn't have sequence points.
            // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
            foreach (var nodeAndPart in EnumerateNearStatements(statement))
            {
                SyntaxNode node = nodeAndPart.Key;
                int part = nodeAndPart.Value;

                if (part == -1)
                {
                    return node.Span;
                }

                if (TryGetActiveSpan(node, part, out span))
                {
                    return span;
                }
            }

            // This might occur in cases where we report rude edit, so the exact location of the active span doens't matter.
            // For example, when a method expression body is removed in C#.
            return statement.Span;
        }

        internal TextSpan GetDeletedNodeActiveSpan(Match<SyntaxNode> match, SyntaxNode deletedNode)
        {
            foreach (var nodeAndPart in EnumerateNearStatements(deletedNode))
            {
                SyntaxNode oldNode = nodeAndPart.Key;
                int part = nodeAndPart.Value;
                if (part == -1)
                {
                    break;
                }

                SyntaxNode newNode;
                if (match.TryGetNewNode(oldNode, out newNode))
                {
                    return FindClosestActiveSpan(newNode, part);
                }
            }

            return GetDeletedNodeDiagnosticSpan(match, deletedNode);
        }

        internal TextSpan GetDeletedNodeDiagnosticSpan(Match<SyntaxNode> match, SyntaxNode deletedNode)
        {
            SyntaxNode newAncestor;
            bool hasAncestor = TryGetMatchingAncestor(match, deletedNode, out newAncestor);
            Debug.Assert(hasAncestor);
            return GetDiagnosticSpan(newAncestor, EditKind.Delete);
        }

        /// <summary>
        /// Finds the inner-most ancestor of the specified node that has a matching node in the new tree.
        /// </summary>
        private static bool TryGetMatchingAncestor(Match<SyntaxNode> match, SyntaxNode oldNode, out SyntaxNode newAncestor)
        {
            while (oldNode != null)
            {
                if (match.TryGetNewNode(oldNode, out newAncestor))
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
            ImmutableArray<ActiveStatementSpan> statements,
            out int start,
            out int end)
        {
            var lines = baseText.Lines;

            // TODO (tomat): use BinarySearch

            int i = 0;
            while (i < statements.Length && !declarationSpan.OverlapsWith(lines.GetTextSpan(statements[i].Span)))
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

            while (i < statements.Length && declarationSpan.OverlapsWith(lines.GetTextSpan(statements[i].Span)))
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
            EditKind parentEdit;
            return editMap.TryGetValue(node, out parentEdit) && parentEdit == editKind;
        }

        #endregion

        #region Rude Edits around Active Statement 

        protected void AddRudeDiagnostic(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode, SyntaxNode newActiveStatement)
        {
            if (oldNode == null)
            {
                AddRudeInsertAroundActiveStatement(diagnostics, newNode);
            }
            else if (newNode == null)
            {
                AddRudeDeleteAroundActiveStatement(diagnostics, oldNode, newActiveStatement);
            }
            else
            {
                AddRudeUpdateAroundActiveStatement(diagnostics, newNode);
            }
        }

        protected void AddRudeUpdateAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT,
                GetDiagnosticSpan(newNode, EditKind.Update),
                newNode,
                new[] { GetStatementDisplayName(newNode, EditKind.Update) }));
        }

        protected void AddRudeInsertAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.RUDE_EDIT_INSERT_AROUND,
                GetDiagnosticSpan(newNode, EditKind.Insert),
                newNode,
                new[] { GetStatementDisplayName(newNode, EditKind.Insert) }));
        }

        protected void AddRudeDeleteAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newActiveStatement)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.RUDE_EDIT_DELETE_AROUND,
                newActiveStatement.Span,
                oldNode,
                new[] { GetStatementDisplayName(oldNode, EditKind.Delete) }));
        }

        protected void ReportUnmatchedStatements<TSyntaxNode>(
            List<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            int syntaxKind,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement,
            Func<TSyntaxNode, TSyntaxNode, bool> areEquivalent,
            Func<TSyntaxNode, TSyntaxNode, bool> areSimilar)
            where TSyntaxNode : SyntaxNode
        {
            List<SyntaxNode> oldNodes = null, newNodes = null;
            GetAncestors(GetEncompassingAncestor(match.OldRoot), oldActiveStatement, syntaxKind, ref oldNodes);
            GetAncestors(GetEncompassingAncestor(match.NewRoot), newActiveStatement, syntaxKind, ref newNodes);

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
            int oldNodeCount = (oldNodes != null) ? oldNodes.Count : 0;

            for (int i = 0; i < newNodes.Count; i++)
            {
                var newNode = newNodes[i];

                if (newNode != null)
                {
                    // Any difference can be expressed as insert, delete & insert, edit, or move & edit.
                    // Heristic: If the nesting levels of the old and new nodes are the same we report an edit.
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
            int matchCount = 0;
            int oldIndex = 0;
            for (int newIndex = 0; newIndex < newNodes.Count; newIndex++)
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

                int i = -1;
                SyntaxNode partner;
                if (match == null)
                {
                    i = IndexOfEquivalent(newNode, oldNodes, oldIndex, comparer);
                }
                else if (match.TryGetOldNode(newNode, out partner) && comparer((TSyntaxNode)partner, (TSyntaxNode)newNode))
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
            for (int i = startIndex; i < oldNodes.Count; i++)
            {
                var oldNode = oldNodes[i];
                if (oldNode != null && comparer((TSyntaxNode)oldNode, (TSyntaxNode)newNode))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void GetAncestors(SyntaxNode root, SyntaxNode node, int syntaxKind, ref List<SyntaxNode> list)
        {
            while (node != root)
            {
                if (node.RawKind == syntaxKind)
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

        // internal for testing
        internal void AnalyzeTrivia(
            SourceText oldSource,
            SourceText newSource,
            Match<SyntaxNode> topMatch,
            Dictionary<SyntaxNode, EditKind> editMap,
            [Out]List<KeyValuePair<SyntaxNode, SyntaxNode>> triviaEdits,
            [Out]List<LineChange> lineEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            foreach (var entry in topMatch.Matches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var oldNode = entry.Key;
                var newNode = entry.Value;

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
                bool requiresUpdate = false;
                bool isFirstToken = true;
                int firstTokenLineDelta = 0;
                LineChange firstTokenLineChange = default(LineChange);
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

                    int lineDelta = oldPosition.Line - newPosition.Line;
                    if (isFirstToken)
                    {
                        isFirstToken = false;
                        firstTokenLineDelta = lineDelta;
                        firstTokenLineChange = (lineDelta != 0) ? new LineChange(oldPosition.Line, newPosition.Line) : default(LineChange);
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
                    triviaEdits.Add(entry);

                    var currentToken = newTokensEnum.Current;

                    TextSpan triviaSpan = TextSpan.FromBounds(
                        previousNewToken.HasTrailingTrivia ? previousNewToken.Span.End : currentToken.FullSpan.Start,
                        currentToken.SpanStart);

                    ReportMemberUpdateRudeEdits(diagnostics, entry.Value, triviaSpan);
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

        // internal for testing
        internal void AnalyzeSemantics(
            EditScript<SyntaxNode> editScript,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            List<KeyValuePair<SyntaxNode, SyntaxNode>> triviaEdits,
            List<ValueTuple<int, IReadOnlyDictionary<SyntaxNode, SyntaxNode>>> updatedActiveMethodMatches,
            SemanticModel oldModel,
            SemanticModel newModel,
            [Out]List<SemanticEdit> semanticEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            // { new type -> old type }
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> instanceConstructorUpdates = null;
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> staticConstructorUpdates = null;

            INamedTypeSymbol layoutAttribute = null;
            var newSymbolsWithEdit = new HashSet<ISymbol>();
            int updatedActiveMethodMatchIndex = 0;
            for (int i = 0; i < editScript.Edits.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var edit = editScript.Edits[i];

                ISymbol oldSymbol, newSymbol;
                Func<SyntaxNode, SyntaxNode> syntaxMap;
                SemanticEditKind editKind;

                switch (edit.Kind)
                {
                    case EditKind.Move:
                        // Move is always a Rude Edit.
                        throw ExceptionUtilities.Unreachable;

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

                            // Deleting an parameterless constructor needs special handling:
                            // If the new type has a parameterless ctor of the same accessibility then UPDATE.
                            // Error otherwise.

                            Debug.Assert(AsParameterlessConstructor(oldSymbol) != null);

                            SyntaxNode oldTypeSyntax = TryGetContainingTypeDeclaration(edit.OldNode);
                            Debug.Assert(oldTypeSyntax != null);

                            var newType = TryGetPartnerType(oldTypeSyntax, editScript.Match, newModel, cancellationToken);

                            newSymbol = TryGetParameterlessConstructor(newType, oldSymbol.IsStatic);
                            if (newSymbol == null || newSymbol.DeclaredAccessibility != oldSymbol.DeclaredAccessibility)
                            {
                                diagnostics.Add(new RudeEditDiagnostic(
                                    RudeEditKind.Delete,
                                    GetDeletedNodeDiagnosticSpan(editScript.Match, edit.OldNode),
                                    edit.OldNode,
                                    new[] { GetTopLevelDisplayName(edit.OldNode, EditKind.Delete) }));

                                continue;
                            }

                            editKind = SemanticEditKind.Update;
                            syntaxMap = null;
                        }

                        break;

                    case EditKind.Reorder:
                        // Currently we don't do any semantic checks for reordering
                        // and we don't need to report them to the compiler either.

                        // Reordering of fields is not allowed since it changes the layout of the type.
                        Debug.Assert(!HasInitializer(edit.OldNode) && !HasInitializer(edit.NewNode));
                        continue;

                    case EditKind.Insert:
                        {
                            editKind = SemanticEditKind.Insert;

                            SyntaxNode newTypeSyntax = TryGetContainingTypeDeclaration(edit.NewNode);

                            if (newTypeSyntax != null && HasEdit(editMap, newTypeSyntax, EditKind.Insert))
                            {
                                // inserting into a new type
                                continue;
                            }

                            syntaxMap = null;
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

                            bool isStatic;
                            bool hasFieldInitializer = HasInitializer(edit.NewNode, out isStatic);

                            if (hasFieldInitializer)
                            {
                                // Insertion of a field with an initializer forces the corresponding constructors to be recompiled.
                                var newType = (INamedTypeSymbol)newModel.GetDeclaredSymbol(newTypeSyntax, cancellationToken);
                                var oldType = TryGetPartnerType(newTypeSyntax, editScript.Match, oldModel, cancellationToken);

                                // There has to be a matching old type syntax since the parent hasn't been inserted.
                                Debug.Assert(oldType != null);

                                ForceConstructorUpdate(oldType, newType, edit.NewNode.Span, isStatic, ref instanceConstructorUpdates, ref staticConstructorUpdates, diagnostics);
                                ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, edit.NewNode, newModel, ref layoutAttribute);
                                break;
                            }

                            // Inserting a parameterless constructor needs special handling:
                            // 1) static ctor
                            //    a) old type has an implicit static ctor
                            //       UPDATE of the implicit static ctor
                            //    b) otherwise
                            //       INSERT of a static parameterless ctor
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

                            IMethodSymbol newCtor = AsParameterlessConstructor(newSymbol);
                            if (newCtor != null)
                            {
                                var oldType = TryGetPartnerType(newTypeSyntax, editScript.Match, oldModel, cancellationToken);

                                // There has to be a matching old type syntax since the containing type hasn't been inserted.
                                Debug.Assert(oldType != null);

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
                            else
                            {
                                ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol);
                                ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, edit.NewNode, newModel, ref layoutAttribute);
                            }
                        }

                        break;

                    case EditKind.Update:
                        {
                            editKind = SemanticEditKind.Update;
                            bool isStatic;
                            if (HasInitializer(edit.OldNode, out isStatic) || HasInitializer(edit.NewNode, out isStatic))
                            {
                                var newInitType = (INamedTypeSymbol)newModel.GetDeclaredSymbol(TryGetContainingTypeDeclaration(edit.NewNode), cancellationToken);
                                var oldInitType = (INamedTypeSymbol)oldModel.GetDeclaredSymbol(TryGetContainingTypeDeclaration(edit.OldNode), cancellationToken);

                                ForceConstructorUpdate(oldInitType, newInitType, edit.NewNode.Span, isStatic, ref instanceConstructorUpdates, ref staticConstructorUpdates, diagnostics);

                                // There is no action the compiler needs to take in addition 
                                // to updating the corresponding constructor.
                                continue;
                            }

                            newSymbol = GetSymbolForEdit(newModel, edit.NewNode, edit.Kind, editMap, cancellationToken);
                            if (newSymbol == null)
                            {
                                // node doesn't represent a symbol
                                continue;
                            }

                            oldSymbol = GetSymbolForEdit(oldModel, edit.OldNode, edit.Kind, editMap, cancellationToken);
                            Debug.Assert((newSymbol == null) == (oldSymbol == null));

                            // this edit is an active method update:
                            if (updatedActiveMethodMatchIndex < updatedActiveMethodMatches.Count &&
                                updatedActiveMethodMatches[updatedActiveMethodMatchIndex].Item1 == i)
                            {
                                var reverseMap = updatedActiveMethodMatches[updatedActiveMethodMatchIndex].Item2;
                                syntaxMap = CreateSyntaxMap(reverseMap);
                                updatedActiveMethodMatchIndex++;
                            }
                            else
                            {
                                syntaxMap = null;
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                semanticEdits.Add(new SemanticEdit(editKind, oldSymbol, newSymbol, syntaxMap, preserveLocalVariables: syntaxMap != null));
                newSymbolsWithEdit.Add(newSymbol);
            }

            foreach (var edit in triviaEdits)
            {
                var oldSymbol = oldModel.GetDeclaredSymbol(edit.Key, cancellationToken);
                var newSymbol = newModel.GetDeclaredSymbol(edit.Value, cancellationToken);

                if (IsMethod(edit.Key))
                {
                    int start, end;

                    bool preserveLocalVariables =
                        TryGetOverlappingActiveStatements(oldText, edit.Key.Span, oldActiveStatements, out start, out end) ||
                        IsStateMachineMethod(edit.Key) ||
                        ContainsLambda(edit.Key);

                    var syntaxMap = preserveLocalVariables ? CreateSyntaxMapForEquivalentNodes(edit.Key, edit.Value) : null;

                    semanticEdits.Add(new SemanticEdit(SemanticEditKind.Update, oldSymbol, newSymbol, syntaxMap, preserveLocalVariables));
                    newSymbolsWithEdit.Add(newSymbol);
                }
                else
                {
                    // we don't track trivia changes outside of method bodies and field/property initializers
                    Debug.Assert(HasInitializer(edit.Key) || HasInitializer(edit.Value));
                    ForceConstructorUpdate(oldSymbol.ContainingType, newSymbol.ContainingType, edit.Value.Span, newSymbol.IsStatic, ref instanceConstructorUpdates, ref staticConstructorUpdates, diagnostics);
                }
            }

            if (instanceConstructorUpdates != null)
            {
                AddConstructorUpdates(
                    instanceConstructorUpdates,
                    editScript.Match,
                    oldText,
                    oldModel,
                    oldActiveStatements,
                    newSymbolsWithEdit,
                    isStatic: false,
                    semanticEdits: semanticEdits,
                    diagnostics: diagnostics,
                    cancellationToken: cancellationToken);
            }

            if (staticConstructorUpdates != null)
            {
                AddConstructorUpdates(
                    staticConstructorUpdates,
                    editScript.Match,
                    oldText,
                    oldModel,
                    oldActiveStatements,
                    newSymbolsWithEdit,
                    isStatic: true,
                    semanticEdits: semanticEdits,
                    diagnostics: diagnostics,
                    cancellationToken: cancellationToken);
            }
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
            bool intoStruct = symbol.ContainingType.TypeKind == TypeKind.Struct;

            diagnostics.Add(new RudeEditDiagnostic(
                intoStruct ? RudeEditKind.InsertIntoStruct : RudeEditKind.InsertIntoClassWithLayout,
                syntax.Span,
                syntax,
                new[]
                {
                    GetTopLevelDisplayName(syntax, EditKind.Insert),
                    GetTopLevelDisplayName(TryGetContainingTypeDeclaration(syntax), EditKind.Update)
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
                layoutAttribute = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
                if (layoutAttribute == null)
                {
                    return false;
                }
            }

            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass.Equals(layoutAttribute) && attribute.ConstructorArguments.Length == 1)
                {
                    object layoutValue = attribute.ConstructorArguments.Single().Value;
                    return (layoutValue is int ? (int)layoutValue :
                            layoutValue is short ? (short)layoutValue :
                            (int)LayoutKind.Auto) != (int)LayoutKind.Auto;
                }
            }

            return false;
        }

        #endregion

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

        private void ForceConstructorUpdate(
            INamedTypeSymbol oldType,
            INamedTypeSymbol newType,
            TextSpan span,
            bool isStatic,
            ref Dictionary<INamedTypeSymbol, INamedTypeSymbol> instanceConstructorUpdates,
            ref Dictionary<INamedTypeSymbol, INamedTypeSymbol> staticConstructorUpdates,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            Debug.Assert(oldType != null);
            Debug.Assert(newType != null);

            if (IsPartial(newType))
            {
                // rude edit: Editing a field initializer of a partial type.
                diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.PartialTypeInitializerUpdate, span));
                return;
            }

            Dictionary<INamedTypeSymbol, INamedTypeSymbol> constructorUpdates;
            if (isStatic)
            {
                constructorUpdates = staticConstructorUpdates ??
                    (staticConstructorUpdates = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>());
            }
            else
            {
                constructorUpdates = instanceConstructorUpdates ??
                    (instanceConstructorUpdates = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>());
            }

            if (!constructorUpdates.ContainsKey(newType))
            {
                constructorUpdates.Add(newType, oldType);
            }
        }

        private void AddConstructorUpdates(
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> updates,
            Match<SyntaxNode> topMatch,
            SourceText oldText,
            SemanticModel oldModel,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            HashSet<ISymbol> newSymbolsWithEdit,
            bool isStatic,
            [Out]List<SemanticEdit> semanticEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            foreach (var entry in updates)
            {
                var newType = entry.Key;
                var oldType = entry.Value;
                Debug.Assert(oldType != null);

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
                        var newDeclaration = newCtor.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);

                        // Partial type field initializers were filtered out previously and rude edits reported.
                        Debug.Assert(newDeclaration.SyntaxTree == topMatch.NewRoot.SyntaxTree);

                        if (!IncludesInitializers(newDeclaration))
                        {
                            continue;
                        }

                        SyntaxNode oldDeclaration;
                        if (!topMatch.TryGetOldNode(newDeclaration, out oldDeclaration))
                        {
                            // new constructor inserted, we don't need to update it
                            continue;
                        }

                        // TODO (tomat): report a better location (perhaps the first updated field initializer?)
                        int diagnosticCount = diagnostics.Count;
                        ReportMemberUpdateRudeEdits(diagnostics, newDeclaration, span: null);
                        if (diagnostics.Count > diagnosticCount)
                        {
                            continue;
                        }

                        oldCtor = oldModel.GetDeclaredSymbol(oldDeclaration, cancellationToken);
                    }
                    else
                    {
                        oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                    }

                    // Note that active statements in field initializers don't require us to preserve locals
                    // of the constructor. They don't contain any local declarators that span statements (explicit or temp).
                    // And the constructor body haven't changed.

                    // TODO: preserve local variables if the constructor or the initializers contain lambdas
                    semanticEdits.Add(new SemanticEdit((oldCtor == null) ? SemanticEditKind.Insert : SemanticEditKind.Update, oldCtor, newCtor));
                }
            }
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

        private static Func<SyntaxNode, SyntaxNode> CreateSyntaxMap(IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
        {
            return newNode =>
            {
                SyntaxNode oldNode;
                return reverseMap.TryGetValue(newNode, out oldNode) ? oldNode : null;
            };
        }

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
                span = default(TextSpan);
                return false;
            }

            int start = lines[lineSpan.Start.Line].Start + lineSpan.Start.Character;
            int end = lines[lineSpan.End.Line].Start + lineSpan.End.Character;
            span = TextSpan.FromBounds(start, end);
            return true;
        }

        #endregion
    }
}
