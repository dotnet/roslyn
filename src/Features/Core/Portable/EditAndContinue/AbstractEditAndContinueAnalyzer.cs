// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
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

        protected abstract TextSpan GetDiagnosticSpan(SyntaxNode node, EditKind editKind);
        internal abstract TextSpan GetLambdaParameterDiagnosticSpan(SyntaxNode lambda, int ordinal);
        protected abstract string GetTopLevelDisplayName(SyntaxNode node, EditKind editKind);
        protected abstract string GetStatementDisplayName(SyntaxNode node, EditKind editKind);
        protected abstract string GetLambdaDisplayName(SyntaxNode lambda);
        protected abstract SymbolDisplayFormat ErrorDisplayFormat { get; }
        protected abstract List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, bool isLeaf);
        protected abstract void GetStateMachineInfo(SyntaxNode body, out ImmutableArray<SyntaxNode> suspensionPoints, out StateMachineKind kind);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportSyntacticRudeEdits(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(List<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, TextSpan newStatementSpan);
        internal abstract void ReportOtherRudeEditsAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode oldStatement, SyntaxNode newStatement, bool isLeaf);
        internal abstract void ReportMemberUpdateRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode newMember, TextSpan? span);
        internal abstract void ReportInsertedMemberSymbolRudeEdits(List<RudeEditDiagnostic> diagnostics, ISymbol newSymbol);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract bool IsMethod(SyntaxNode declaration);
        internal abstract bool IsLambda(SyntaxNode node);
        internal abstract bool IsLambdaExpression(SyntaxNode node);
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
        internal abstract SyntaxNode TryGetContainingTypeDeclaration(SyntaxNode memberDeclaration);

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
                    trackingService,
                    baseActiveStatements,
                    newActiveStatements,
                    newExceptionRegions,
                    updatedMethods,
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
                        new RudeEditDiagnostic(RudeEditKind.InsertFile, default(TextSpan))));
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
                        updatedMethods,
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
            [Out]List<UpdatedMemberInfo> updatedMethods,
            [Out]List<RudeEditDiagnostic> diagnostics)
        {
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);
            Debug.Assert(updatedMethods != null);
            Debug.Assert(updatedMethods.Count == 0);

            var updatedTrackingSpans = new List<KeyValuePair<ActiveStatementId, TextSpan>>();

            for (int i = 0; i < script.Edits.Length; i++)
            {
                var edit = script.Edits[i];

                AnalyzeUpdatedActiveMethodBodies(script, i, editMap, oldText, newText, documentId, trackingService, oldActiveStatements, newActiveStatements, newExceptionRegions, updatedMethods, updatedTrackingSpans, diagnostics);
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

                    // Finds a matching syntax node in the new source.
                    // In case the node got deleted the newMember may be missing.
                    // For those the active span should remain empty.
                    SyntaxNode newMember;
                    bool hasPartner = topMatch.TryGetNewNode(oldMember, out newMember);
                    Debug.Assert(hasPartner || trackedSpan.IsEmpty);
                    if (!hasPartner)
                    {
                        continue;
                    }

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

        internal struct UpdatedMemberInfo
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

            // The method body has a suspension point (await/yield); 
            // only true if the body itself has the suspension point, not if it contains async/iterator lambda
            public readonly bool HasStateMachineSuspensionPoint;

            public UpdatedMemberInfo(
                int editOrdinal,
                SyntaxNode oldBody,
                SyntaxNode newBody,
                BidirectionalMap<SyntaxNode> map,
                IReadOnlyDictionary<SyntaxNode, LambdaInfo> activeOrMatchedLambdasOpt,
                bool hasActiveStatement,
                bool hasStateMachineSuspensionPoint)
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
                HasStateMachineSuspensionPoint = hasStateMachineSuspensionPoint;
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
            [Out]List<UpdatedMemberInfo> updatedMembers,
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
                    var newSpan = IsDeclarationWithInitializer(edit.OldNode) ?
                        GetDeletedNodeActiveSpan(topEditScript.Match.Matches, edit.OldNode) :
                        GetDeletedNodeDiagnosticSpan(topEditScript.Match.Matches, edit.OldNode);

                    for (int i = start; i < end; i++)
                    {
                        // TODO: VB field multi-initializers break this
                        // Debug.Assert(newActiveStatements[i] == default(LinePositionSpan));

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
            // { old-lambda-body -> info }
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
            var bodyMatch = ComputeBodyMatch(oldBody, newBody, activeNodes.Where(n => n.EnclosingLambdaBodyOpt == null).ToArray(), diagnostics, out hasStateMachineSuspensionPoint);
            var map = ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas, diagnostics);

            // Save the body match for local variable mapping.
            // We'll use it to tell the compiler what local variables to preserve in an active method.
            // An edited async/iterator method is considered active.
            updatedMembers.Add(new UpdatedMemberInfo(editOrdinal, oldBody, newBody, map, lazyActiveOrMatchedLambdas, hasActiveStatement, hasStateMachineSuspensionPoint));

            for (int i = 0; i < activeNodes.Length; i++)
            {
                int ordinal = start + i;
                bool hasMatching = false;
                bool isLeaf = (oldActiveStatements[ordinal].Flags & ActiveStatementFlags.LeafFrame) != 0;
                bool isPartiallyExecuted = (oldActiveStatements[ordinal].Flags & ActiveStatementFlags.PartiallyExecuted) != 0;
                int statementPart = activeNodes[i].StatementPart;
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
                    SyntaxNode newEnclosingLambdaBody = oldLambdaInfo.NewBody;
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

                    if ((!isLeaf || isPartiallyExecuted) && !AreEquivalentActiveStatements(oldStatementSyntax, newStatementSyntaxOpt, statementPart))
                    {
                        // rude edit: internal active statement changed
                        diagnostics.Add(new RudeEditDiagnostic(isLeaf ? RudeEditKind.PartiallyExecutedActiveStatementUpdate : RudeEditKind.ActiveStatementUpdate, newSpan));
                    }

                    // other statements around active statement:
                    ReportOtherRudeEditsAroundActiveStatement(diagnostics, match, oldStatementSyntax, newStatementSyntaxOpt, isLeaf);
                }
                else if (match == null)
                {
                    Debug.Assert(oldEnclosingLambdaBody != null);

                    newSpan = GetDeletedNodeDiagnosticSpan(oldEnclosingLambdaBody, bodyMatch, lazyActiveOrMatchedLambdas);

                    // Lambda containing the active statement can't be found in the new source.
                    var oldLambda = GetLambda(oldEnclosingLambdaBody);
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ActiveStatementLambdaRemoved, newSpan, oldLambda,
                        new[] { GetLambdaDisplayName(oldLambda) }));
                }
                else
                {
                    newSpan = GetDeletedNodeActiveSpan(match.Matches, oldStatementSyntax);

                    if (!isLeaf || isPartiallyExecuted)
                    {
                        // rude edit: internal active statement deleted
                        diagnostics.Add(
                            new RudeEditDiagnostic(isLeaf ? RudeEditKind.PartiallyExecutedActiveStatementDelete : RudeEditKind.DeleteActiveStatement,
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
                    isLeaf,
                    newExceptionRegions,
                    diagnostics);

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

        private void CalculateExceptionRegionsAroundActiveStatement(
            Match<SyntaxNode> bodyMatch,
            SyntaxNode oldStatementSyntax,
            SyntaxNode newStatementSyntaxOpt,
            TextSpan newStatementSyntaxSpan,
            int ordinal,
            SourceText newText,
            bool isLeaf,
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

            var oldAncestors = GetExceptionHandlingAncestors(oldStatementSyntax, isLeaf);
            var newAncestors = GetExceptionHandlingAncestors(newStatementSyntaxOpt, isLeaf);

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
        /// Internal for testing.
        /// </summary>
        internal BidirectionalMap<SyntaxNode> ComputeMap(
            Match<SyntaxNode> bodyMatch,
            ActiveNode[] activeNodes,
            ref Dictionary<SyntaxNode, LambdaInfo> lazyActiveOrMatchedLambdas,
            List<RudeEditDiagnostic> diagnostics)
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

                        SyntaxNode newLambdaBody1 = TryGetPartnerLambdaBody(oldLambdaBody1, newLambda);
                        if (newLambdaBody1 != null)
                        {
                            lambdaBodyMatches.Add(ComputeLambdaBodyMatch(oldLambdaBody1, newLambdaBody1, activeNodes, lazyActiveOrMatchedLambdas, diagnostics));
                        }

                        if (oldLambdaBody2 != null)
                        {
                            SyntaxNode newLambdaBody2 = TryGetPartnerLambdaBody(oldLambdaBody2, newLambda);
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
                        pair.Key == lambdaBodyMatch.OldRoot && pair.Value == lambdaBodyMatch.NewRoot && IsLambda(pair.Key));

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
            LambdaInfo info;
            if (activeOrMatchedLambdas.TryGetValue(oldLambdaBody, out info))
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
            var lambdaBodyMatch = ComputeBodyMatch(oldLambdaBody, newLambdaBody, activeNodesInLambda ?? SpecializedCollections.EmptyArray<ActiveNode>(), diagnostics, out needsSyntaxMap);

            activeOrMatchedLambdas[oldLambdaBody] = info.WithMatch(lambdaBodyMatch, newLambdaBody);

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

            ImmutableArray<SyntaxNode> oldStateMachineSuspensionPoints, newStateMachineSuspensionPoints;
            StateMachineKind oldStateMachineKind, newStateMachineKind;

            GetStateMachineInfo(oldBody, out oldStateMachineSuspensionPoints, out oldStateMachineKind);
            GetStateMachineInfo(newBody, out newStateMachineSuspensionPoints, out newStateMachineKind);

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
                            GetDeletedNodeDiagnosticSpan(match.Matches, deletedNode),
                            deletedNode,
                            new[] { GetStatementDisplayName(deletedNode, EditKind.Delete) }));
                    }
                    else
                    {
                        Debug.Assert(rudeEdit.Kind == EditKind.Insert);

                        var insertedNode = newStateMachineSuspensionPoints[rudeEdit.NewIndex];

                        diagnostics.Add(new RudeEditDiagnostic(
                            creatingStateMachineAroundActiveStatement ? RudeEditKind.InsertAroundActiveStatement : RudeEditKind.Insert,
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
            else if (activeNodes.Length > 0)
            {
                // It is allow to update a regular method to an async method or an iterator.
                // The only restriction is a presence of an active statement in the method body
                // since the debugger does not support remapping active statements to a different method.
                if (oldStateMachineKind == StateMachineKind.None && newStateMachineKind != StateMachineKind.None )
                {                    
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement,
                        GetDiagnosticSpan(IsMethod(newBody) ? newBody : newBody.Parent, EditKind.Update)));
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

        private TextSpan GetDeletedNodeDiagnosticSpan(SyntaxNode deletedLambdaBody, Match<SyntaxNode> match, Dictionary<SyntaxNode, LambdaInfo> lambdaInfos)
        {
            SyntaxNode oldLambdaBody = deletedLambdaBody;
            while (true)
            {
                var oldParentLambdaBody = FindEnclosingLambdaBody(match.OldRoot, GetLambda(oldLambdaBody));
                if (oldParentLambdaBody == null)
                {
                    return GetDeletedNodeDiagnosticSpan(match.Matches, oldLambdaBody);
                }

                LambdaInfo lambdaInfo;
                if (lambdaInfos.TryGetValue(oldParentLambdaBody, out lambdaInfo) && lambdaInfo.Match != null)
                {
                    return GetDeletedNodeDiagnosticSpan(lambdaInfo.Match.Matches, oldLambdaBody);
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

            // This might occur in cases where we report rude edit, so the exact location of the active span doesn't matter.
            // For example, when a method expression body is removed in C#.
            return statement.Span;
        }

        internal TextSpan GetDeletedNodeActiveSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedNode)
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
                if (forwardMap.TryGetValue(oldNode, out newNode))
                {
                    return FindClosestActiveSpan(newNode, part);
                }
            }

            return GetDeletedNodeDiagnosticSpan(forwardMap, deletedNode);
        }

        internal TextSpan GetDeletedNodeDiagnosticSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedNode)
        {
            SyntaxNode newAncestor;
            bool hasAncestor = TryGetMatchingAncestor(forwardMap, deletedNode, out newAncestor);
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

        protected void AddRudeDiagnostic(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode, TextSpan newActiveStatementSpan)
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
                new[] { GetStatementDisplayName(newNode, EditKind.Update) }));
        }

        protected void AddRudeInsertAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.InsertAroundActiveStatement,
                GetDiagnosticSpan(newNode, EditKind.Insert),
                newNode,
                new[] { GetStatementDisplayName(newNode, EditKind.Insert) }));
        }

        protected void AddRudeDeleteAroundActiveStatement(List<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, TextSpan newActiveStatementSpan)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.DeleteAroundActiveStatement,
                newActiveStatementSpan,
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

        private struct ConstructorEdit
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

        // internal for testing
        internal void AnalyzeSemantics(
            EditScript<SyntaxNode> editScript,
            Dictionary<SyntaxNode, EditKind> editMap,
            SourceText oldText,
            ImmutableArray<ActiveStatementSpan> oldActiveStatements,
            List<KeyValuePair<SyntaxNode, SyntaxNode>> triviaEdits,
            List<UpdatedMemberInfo> updatedMembers,
            SemanticModel oldModel,
            SemanticModel newModel,
            [Out]List<SemanticEdit> semanticEdits,
            [Out]List<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            // { new type -> constructor update }
            Dictionary<INamedTypeSymbol, ConstructorEdit> instanceConstructorEdits = null;
            Dictionary<INamedTypeSymbol, ConstructorEdit> staticConstructorEdits = null;

            INamedTypeSymbol layoutAttribute = null;
            var newSymbolsWithEdit = new HashSet<ISymbol>();
            int updatedMemberIndex = 0;
            for (int i = 0; i < editScript.Edits.Length; i++)
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
                                    GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, edit.OldNode),
                                    edit.OldNode,
                                    new[] { GetTopLevelDisplayName(edit.OldNode, EditKind.Delete) }));

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

                            SyntaxNode newTypeSyntax = TryGetContainingTypeDeclaration(edit.NewNode);

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

                            if (updatedMemberIndex < updatedMembers.Count && updatedMembers[updatedMemberIndex].EditOrdinal == i)
                            {
                                var updatedMember = updatedMembers[updatedMemberIndex];

                                bool newBodyHasLambdas;
                                ReportLambdaAndClosureRudeEdits(
                                    oldModel,
                                    updatedMember.OldBody,
                                    oldSymbol,
                                    newModel,
                                    updatedMember.NewBody,
                                    newSymbol,
                                    updatedMember.ActiveOrMatchedLambdasOpt,
                                    updatedMember.Map,
                                    diagnostics,
                                    out newBodyHasLambdas,
                                    cancellationToken);

                                // We need to provide syntax map to the compiler if 
                                // 1) The new member has a active statement
                                //    The values of local variables declared or synthesized in the method have to be preserved.
                                // 2) The new member generates a state machine 
                                //    In case the state machine is suspended we need to preserve variables.
                                // 3) The new member contains lambdas
                                //    We need to map new lambdas in the method to the matching old ones. 
                                //    If the old method has lambdas but the new one doesn't there is nothing to preserve.
                                if (updatedMember.HasActiveStatement || updatedMember.HasStateMachineSuspensionPoint || newBodyHasLambdas)
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
                                    oldSymbol.ContainingType,
                                    newSymbol.ContainingType,
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
                        throw ExceptionUtilities.Unreachable;
                }

                semanticEdits.Add(new SemanticEdit(editKind, oldSymbol, newSymbol, syntaxMapOpt, preserveLocalVariables: syntaxMapOpt != null));
                newSymbolsWithEdit.Add(newSymbol);
            }

            foreach (var edit in triviaEdits)
            {
                var oldSymbol = GetSymbolForEdit(oldModel, edit.Key, EditKind.Update, editMap, cancellationToken);
                var newSymbol = GetSymbolForEdit(newModel, edit.Value, EditKind.Update, editMap, cancellationToken);

                int start, end;

                // We need to provide syntax map to the compiler if the member is active (see member update above):
                bool isActiveMember =
                    TryGetOverlappingActiveStatements(oldText, edit.Key.Span, oldActiveStatements, out start, out end) ||
                    IsStateMachineMethod(edit.Key) ||
                    ContainsLambda(edit.Key);

                var syntaxMap = isActiveMember ? CreateSyntaxMapForEquivalentNodes(edit.Key, edit.Value) : null;

                // only trivia changed:
                Debug.Assert(IsConstructorWithMemberInitializers(edit.Key) == IsConstructorWithMemberInitializers(edit.Value));
                Debug.Assert(IsDeclarationWithInitializer(edit.Key) == IsDeclarationWithInitializer(edit.Value));

                bool isConstructorWithMemberInitializers;
                if ((isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(edit.Value)) ||
                    IsDeclarationWithInitializer(edit.Value))
                {
                    if (DeferConstructorEdit(
                        oldSymbol.ContainingType,
                        newSymbol.ContainingType,
                        SemanticEditKind.Update,
                        edit.Value,
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
                    oldText,
                    oldModel,
                    oldActiveStatements,
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
                SyntaxNode oldNode;
                return reverseMap.TryGetValue(newNode, out oldNode) ? oldNode : null;
            };
        }

        private Func<SyntaxNode, SyntaxNode> CreateSyntaxMapForPartialTypeConstructor(
            INamedTypeSymbol oldType,
            INamedTypeSymbol newType,
            SemanticModel newModel,
            Func<SyntaxNode, SyntaxNode> ctorSyntaxMapOpt)
        {
            return newNode => ctorSyntaxMapOpt?.Invoke(newNode) ?? FindPartnerInMemberInitializer(newModel, newType, newNode, oldType, default(CancellationToken));
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
                Func<SyntaxNode, SyntaxNode> syntaxMapOpt;
                if (changedDeclarations.TryGetValue(newDeclaration, out syntaxMapOpt))
                {
                    // If syntax map is not available the declaration was either
                    // 1) updated but is not active
                    // 2) inserted
                    return syntaxMapOpt?.Invoke(newNode);
                }

                // The node is in a declaration that hasn't been changed:
                SyntaxNode oldDeclaration;
                if (reverseTopMatches.TryGetValue(newDeclaration, out oldDeclaration))
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
                                        new[] { GetTopLevelDisplayName(newDeclaration, EditKind.Update)}));
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

            ConstructorEdit edit;
            if (!constructorEdits.TryGetValue(newType, out edit))
            {
                constructorEdits.Add(newType, edit = new ConstructorEdit(oldType));
            }

            edit.ChangedDeclarations.Add(newDeclaration, syntaxMapOpt);

            return true;
        }

        private void AddConstructorEdits(
            Dictionary<INamedTypeSymbol, ConstructorEdit> updatedTypes,
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
            foreach (var updatedType in updatedTypes)
            {
                var newType = updatedType.Key;
                var update = updatedType.Value;
                var oldType = update.OldType;

                Debug.Assert(!IsPartial(oldType));
                Debug.Assert(!IsPartial(newType));

                bool anyInitializerUpdates = update.ChangedDeclarations.Keys.Any(IsDeclarationWithInitializer);

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

                        SyntaxNode oldDeclaration;
                        if (topMatch.TryGetOldNode(newDeclaration, out oldDeclaration))
                        {
                            // If the constructor wasn't explicitly edited and its body edit is disallowed report an error.
                            int diagnosticCount = diagnostics.Count;
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
            ISymbol oldMember,
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
                bool anySignatureErrors = false;
                foreach (var entry in matchedLambdasOpt)
                {
                    var oldLambdaBody = entry.Key;
                    var newLambdaBody = entry.Value.NewBody;

                    bool hasErrors;
                    ReportLambdaSignatureRudeEdits(oldModel, oldLambdaBody, newModel, newLambdaBody, diagnostics, out hasErrors, cancellationToken);
                    anySignatureErrors |= hasErrors;
                }

                ArrayBuilder<SyntaxNode> lazyNewErroneousClauses = null;
                foreach (var entry in map.Forward)
                {
                    var oldQueryClause = entry.Key;
                    var newQueryClause = entry.Value;

                    if (!QueryClauseLambdasTypeEquivalent(oldModel, oldQueryClause, newModel, newQueryClause, cancellationToken))
                    {
                        lazyNewErroneousClauses = lazyNewErroneousClauses ?? ArrayBuilder<SyntaxNode>.GetInstance();
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
                            new[] { GetStatementDisplayName(newQueryClause, EditKind.Update) }));
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

            bool anyCaptureErrors;

            // { new capture index -> old capture index }
            var reverseCapturesMap = ArrayBuilder<int>.GetInstance(newCaptures.Length, 0);

            // { new capture index -> new closure scope or null for "this" }
            var newCapturesToClosureScopes = ArrayBuilder<SyntaxNode>.GetInstance(newCaptures.Length, null);

            // Can be calculated from other maps but it's simpler to just calculate it upfront.
            // { old capture index -> old closure scope or null for "this" }
            var oldCapturesToClosureScopes = ArrayBuilder<SyntaxNode>.GetInstance(oldCaptures.Length, null);

            CalculateCapturedVariablesMaps(
                oldModel,
                oldCaptures,
                oldMember,
                oldMemberBody,
                newModel,
                newCaptures,
                newMember,
                newMemberBody,
                map,
                reverseCapturesMap,
                newCapturesToClosureScopes,
                oldCapturesToClosureScopes,
                diagnostics,
                out anyCaptureErrors,
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
                bool mappedLambdasHaveErrors = false;
                foreach (var entry in matchedLambdasOpt)
                {
                    var oldLambdaBody = entry.Key;
                    var newLambdaBody = entry.Value.NewBody;

                    BitVector accessedOldCaptures = GetAccessedCaptures(oldLambdaBody, oldModel, oldCaptures, oldCapturesIndex);
                    BitVector accessedNewCaptures = GetAccessedCaptures(newLambdaBody, newModel, newCaptures, newCapturesIndex);

                    // Requirement: 
                    // (new(ReadInside) \/ new(WrittenInside)) /\ new(Captured) == (old(ReadInside) \/ old(WrittenInside)) /\ old(Captured)
                    for (int newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
                    {
                        bool newAccessed = accessedNewCaptures[newCaptureIndex];
                        bool oldAccessed = accessedOldCaptures[reverseCapturesMap[newCaptureIndex]];

                        if (newAccessed != oldAccessed)
                        {
                            var newCapture = newCaptures[newCaptureIndex];

                            var rudeEdit = newAccessed ? RudeEditKind.AccessingCapturedVariableInLambda : RudeEditKind.NotAccessingCapturedVariableInLambda;
                            var arguments = new[] { newCapture.Name, GetLambdaDisplayName(GetLambda(newLambdaBody)) };

                            if (newCapture.IsThisParameter() || oldAccessed)
                            {
                                // changed accessed to "this", or captured variable accessed in old lambda is not accessed in the new lambda
                                diagnostics.Add(new RudeEditDiagnostic(rudeEdit, GetDiagnosticSpan(GetLambda(newLambdaBody), EditKind.Update), null, arguments));
                            }
                            else if (newAccessed)
                            {
                                // captured variable accessed in new lambda is not accessed in the old lambda
                                bool hasUseSites = false;
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

            foreach (var newLambda in newMemberBody.DescendantNodesAndSelf())
            {
                SyntaxNode newLambdaBody1, newLambdaBody2;
                if (TryGetLambdaBodies(newLambda, out newLambdaBody1, out newLambdaBody2))
                {
                    if (!map.Reverse.ContainsKey(newLambda))
                    {
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
                SyntaxNode oldLambdaBody1, oldLambdaBody2;
                if (TryGetLambdaBodies(oldLambda, out oldLambdaBody1, out oldLambdaBody2) && !map.Forward.ContainsKey(oldLambda))
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

            BitVector accessedCaptures = GetAccessedCaptures(lambdaBody, model, captures, capturesIndex);

            int firstAccessedCaptureIndex = -1;
            for (int i = 0; i < captures.Length; i++)
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
                            new[] { GetLambdaDisplayName(GetLambda(lambdaBody)), captures[firstAccessedCaptureIndex].Name, captures[i].Name }));

                        break;
                    }
                }
            }
        }

        private BitVector GetAccessedCaptures(SyntaxNode lambdaBody, SemanticModel model, ImmutableArray<ISymbol> captures, PooledDictionary<ISymbol, int> capturesIndex)
        {
            BitVector result = BitVector.Create(captures.Length);

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
                int newCaptureIndex;
                if (index.TryGetValue(variable, out newCaptureIndex))
                {
                    mask[newCaptureIndex] = true;
                }
            }
        }

        private static void BuildIndex<TKey>(Dictionary<TKey, int> index, ImmutableArray<TKey> array)
        {
            for (int i = 0; i < array.Length; i++)
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
            return local.Locations.First().SourceSpan;
        }

        private static ImmutableArray<IParameterSymbol> GetParametersWithSyntax(ISymbol member)
        {
            var method = (IMethodSymbol)member;

            if (method.AssociatedSymbol != null)
            {
                return ((IPropertySymbol)method.AssociatedSymbol).Parameters;
            }
            else
            {
                return method.Parameters;
            }
        }

        private ValueTuple<SyntaxNode, int> GetParameterKey(IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            var containingLambda = parameter.ContainingSymbol as IMethodSymbol;
            if (containingLambda?.MethodKind == MethodKind.LambdaMethod)
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
            SyntaxNode containingLambdaSyntax = parameterKey.Item1;
            int ordinal = parameterKey.Item2;

            if (containingLambdaSyntax == null)
            {
                // method parameter: no syntax, same ordinal (can't change since method signatures must match)
                mappedParameterKey = parameterKey;
                return true;
            }

            SyntaxNode mappedContainingLambdaSyntax;
            if (map.TryGetValue(containingLambdaSyntax, out mappedContainingLambdaSyntax))
            {
                // parameter of an existing lambda: same ordinal (can't change since lambda signatures must match), 
                mappedParameterKey = ValueTuple.Create(mappedContainingLambdaSyntax, ordinal);
                return true;
            }

            // no mapping
            mappedParameterKey = default(ValueTuple<SyntaxNode, int>);
            return false;
        }

        private void CalculateCapturedVariablesMaps(
            SemanticModel oldModel,
            ImmutableArray<ISymbol> oldCaptures,
            ISymbol oldMember,
            SyntaxNode oldMemberBody,
            SemanticModel newModel,
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

            for (int i = 0; i < oldCaptures.Length; i++)
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

            for (int newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
            {
                ISymbol newCapture = newCaptures[newCaptureIndex];
                int oldCaptureIndex;

                if (newCapture.Kind == SymbolKind.Parameter)
                {
                    var newParameterCapture = (IParameterSymbol)newCapture;
                    var newParameterKey = GetParameterKey(newParameterCapture, cancellationToken);

                    ValueTuple<SyntaxNode, int> oldParameterKey;
                    if (!TryMapParameter(newParameterKey, map.Reverse, out oldParameterKey) ||
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
                    SyntaxNode mappedOldSyntax;

                    var newCaptureSyntax = GetSymbolSyntax(newCapture, cancellationToken);

                    // variable doesn't exists in the old method or has not been captured prior the edit:
                    if (!map.Reverse.TryGetValue(newCaptureSyntax, out mappedOldSyntax) ||
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

                ISymbol oldCapture = oldCaptures[oldCaptureIndex];

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
                SyntaxNode oldScopeOpt = GetCapturedVariableScope(oldCapture, oldMemberBody, cancellationToken);
                SyntaxNode newScopeOpt = GetCapturedVariableScope(newCapture, newMemberBody, cancellationToken);
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
                var newMemberParameters = GetParametersWithSyntax(newMember);

                // uncaptured parameters:
                foreach (var entry in oldParameterCapturesByLambdaAndOrdinal)
                {
                    int ordinal = entry.Key.Item2;
                    var oldContainingLambdaSyntax = entry.Key.Item1;
                    int oldCaptureIndex = entry.Value;
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
                    else
                    {
                        // method or property:
                        span = GetVariableDiagnosticSpan(newMemberParameters[ordinal]);
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
                    SyntaxNode oldCaptureNode = entry.Key;
                    int oldCaptureIndex = entry.Value;
                    var name = oldCaptures[oldCaptureIndex].Name;

                    SyntaxNode newCaptureNode;
                    if (map.Forward.TryGetValue(oldCaptureNode, out newCaptureNode))
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

        private void ReportLambdaSignatureRudeEdits(
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

            Debug.Assert(IsLambdaExpression(newLambda) == IsLambdaExpression(oldLambda));

            // queries are analyzed separately
            if (!IsLambdaExpression(newLambda))
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
                new[] { GetLambdaDisplayName(newLambda) }));

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

            SyntaxNode node = GetSymbolSyntax(localOrParameter, cancellationToken);
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

            SyntaxNode mappedScope;
            return reverseMap.TryGetValue(newScopeOpt, out mappedScope) && mappedScope == oldScopeOpt;
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
