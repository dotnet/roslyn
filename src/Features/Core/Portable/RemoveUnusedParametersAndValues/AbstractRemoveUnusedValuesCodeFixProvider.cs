// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
{
    /// <summary>
    /// Code fixer for unused expression value diagnostics reported by <see cref="AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer"/>.
    /// We provide following code fixes:
    ///     1. If the unused value assigned to a local/parameter has no side-effects,
    ///        we recommend removing the assignment. We consider an expression value to have no side effects
    ///        if one of the following is true:
    ///         1. Value is a compile time constant.
    ///         2. Value is a local or parameter reference.
    ///         3. Value is a field reference with no or implicit this instance.
    ///     2. Otherwise, if user preference is set to DiscardVariable, and project's
    ///        language version supports discard variable, we recommend assigning the value to discard.
    ///     3. Otherwise, we recommend assigning the value to a new unused local variable which has no reads.
    /// </summary>
    internal abstract class AbstractRemoveUnusedValuesCodeFixProvider<TExpressionSyntax, TStatementSyntax, TBlockSyntax,
        TExpressionStatementSyntax, TLocalDeclarationStatementSyntax, TVariableDeclaratorSyntax, TForEachStatementSyntax,
        TSwitchCaseBlockSyntax, TSwitchCaseLabelOrClauseSyntax, TCatchStatementSyntax, TCatchBlockSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TBlockSyntax : TStatementSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatementSyntax : TStatementSyntax
        where TForEachStatementSyntax: TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
        where TSwitchCaseBlockSyntax : SyntaxNode
        where TSwitchCaseLabelOrClauseSyntax: SyntaxNode
    {
        protected const string DiscardVariableName = "_";

        private static readonly SyntaxAnnotation s_memberAnnotation = new SyntaxAnnotation();
        private static readonly SyntaxAnnotation s_newLocalDeclarationStatementAnnotation = new SyntaxAnnotation();
        private static readonly SyntaxAnnotation s_unusedLocalDeclarationAnnotation = new SyntaxAnnotation();

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                                                                                                    IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId);

        /// <summary>
        /// Method to update the identifier token for the local/parameter declaration or reference
        /// that was flagged as an unused value write by the analyzer.
        /// Returns null if the provided node is not one of the handled node kinds.
        /// Otherwise, returns the new node with updated identifier.
        /// </summary>
        /// <param name="node">Flaggged node containing the identifier token to be replaced.</param>
        /// <param name="newName">New identifier token</param>
        protected abstract SyntaxNode TryUpdateNameForFlaggedNode(SyntaxNode node, SyntaxToken newName);

        /// <summary>
        /// Get the identifier token for the iteration variable of the given foreach statement node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected abstract SyntaxToken GetForEachStatementIdentifier(TForEachStatementSyntax node);

        /// <summary>
        /// Wrap the given statements within a block statement.
        /// </summary>
        protected abstract TBlockSyntax GenerateBlock(IEnumerable<TStatementSyntax> statements);

        /// <summary>
        /// Insert the given declaration statement at the start of the given switch case block.
        /// </summary>
        protected abstract void InsertAtStartOfSwitchCaseBlock(TSwitchCaseBlockSyntax switchCaseBlock, SyntaxEditor editor, TLocalDeclarationStatementSyntax declarationStatement);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var preference = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetUnusedValuePreference(diagnostic);
            var isRemovableAssignment = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostic);

            string title;
            if (isRemovableAssignment)
            {
                // Recommend removing the redundant constant value assignment.
                title = FeaturesResources.Remove_redundant_assignment;
            }
            else
            {
                // Recommend using discard/unused local for redundant non-constant assignment.
                switch (preference)
                {
                    case UnusedValuePreference.DiscardVariable:
                        if (IsForEachIterationVariableDiagnostic(diagnostic, context.Document))
                        {
                            // Do not offer a fix to replace unused foreach iteration variable with discard.
                            // User should probably replace it with a for loop based on the collection length.
                            return Task.CompletedTask;
                        }

                        title = FeaturesResources.Use_discard_underscore;
                        break;

                    case UnusedValuePreference.UnusedLocalVariable:
                        title = FeaturesResources.Use_discarded_local;
                        break;

                    default:
                        return Task.CompletedTask;
                }
            }

            context.RegisterCodeFix(
                new MyCodeAction(
                    title,
                    c => FixAsync(context.Document, diagnostic, c),
                    equivalenceKey: GetEquivalenceKey(preference, isRemovableAssignment)),
                diagnostic);

            return Task.CompletedTask;
        }

        private static bool IsForEachIterationVariableDiagnostic(Diagnostic diagnostic, Document document)
        {
            // Do not offer a fix to replace unused foreach iteration variable with discard.
            // User should probably replace it with a for loop based on the collection length.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return syntaxFacts.IsForEachStatement(diagnostic.Location.FindNode(CancellationToken.None));
        }

        private static string GetEquivalenceKey(UnusedValuePreference preference, bool isRemovableAssignment)
            => preference.ToString() + isRemovableAssignment;

        private static string GetEquivalenceKey(Diagnostic diagnostic)
        {
            var preference = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetUnusedValuePreference(diagnostic);
            var isRemovableAssignment = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostic);
            return GetEquivalenceKey(preference, isRemovableAssignment);
        }

        /// <summary>
        /// Flag to indicate if the code fix can introduce local declaration statements
        /// that need to be moved closer to the first reference of the declared variable.
        /// This is currently only possible for the unused value assignment fix.
        /// </summary>
        private static bool NeedsToMoveNewLocalDeclarationsNearReference(string diagnosticId)
            => diagnosticId == IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId;

        protected override bool IncludeDiagnosticDuringFixAll(FixAllState fixAllState, Diagnostic diagnostic)
        {
            return fixAllState.CodeActionEquivalenceKey == GetEquivalenceKey(diagnostic) &&
                !IsForEachIterationVariableDiagnostic(diagnostic, fixAllState.Document);
        }

        private IEnumerable<IGrouping<SyntaxNode, Diagnostic>> GetDiagnosticsGroupedByMember(
            ImmutableArray<Diagnostic> diagnostics,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode root,
            out string diagnosticId,
            out UnusedValuePreference preference,
            out bool removeAssignments)
        {
            diagnosticId = diagnostics[0].Id;
            preference = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetUnusedValuePreference(diagnostics[0]);
            removeAssignments = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostics[0]);
#if DEBUG
            foreach (var diagnostic in diagnostics)
            {
                Debug.Assert(diagnosticId == diagnostic.Id);
                Debug.Assert(preference == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetUnusedValuePreference(diagnostic));
                Debug.Assert(removeAssignments == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostic));
            }
#endif

            return GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root);
        }

        private IEnumerable<IGrouping<SyntaxNode, Diagnostic>> GetDiagnosticsGroupedByMember(
            ImmutableArray<Diagnostic> diagnostics,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode root)
            => diagnostics.GroupBy(d => syntaxFacts.GetContainingMemberDeclaration(root, d.Location.SourceSpan.Start));

        protected override async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            // Track all the member declaration nodes that have diagnostics.
            // We will post process all these tracked nodes after applying the fix (see "PostProcessDocumentAsync" below in this source file).

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var memberDeclarations = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root).Select(g => g.Key);
            root = root.ReplaceNodes(memberDeclarations, computeReplacementNode: (_, n) => n.WithAdditionalAnnotations(s_memberAnnotation));
            document = document.WithSyntaxRoot(root);
            return await base.FixAllAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var diagnosticsGroupedByMember = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root,
                out var diagnosticId, out var preference, out var removeAssignments);
            if (preference == UnusedValuePreference.None)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var usedNames = PooledHashSet<string>.GetInstance();
            try
            {
                foreach (var diagnosticsToFix in diagnosticsGroupedByMember)
                {
                    var orderedDiagnostics = diagnosticsToFix.OrderBy(d => d.Location.SourceSpan.Start);
                    FixAll(diagnosticId, orderedDiagnostics, semanticModel, root, preference,
                        removeAssignments, GenerateUniqueNameAtSpanStart, editor, syntaxFacts, cancellationToken);
                    usedNames.Clear();
                }

                var currentRoot = editor.GetChangedRoot();
                var newRoot = await PostProcessDocumentAsync(document, currentRoot,
                    diagnosticId, preference, cancellationToken).ConfigureAwait(false);
                if (currentRoot != newRoot)
                {
                    editor.ReplaceNode(root, newRoot);
                }
            }
            finally
            {
                usedNames.Free();
            }

            return;

            // Local functions
            string GenerateUniqueNameAtSpanStart(SyntaxNode node)
            {
                var localsInNestedScope = PooledHashSet<string>.GetInstance();
                try
                {
                    // Add local names for all variable declarations in nested scopes for this node.
                    // This helps prevent name clashes with locals declared in nested block scopes.
                    AddLocalsInNestedScope(node, localsInNestedScope);

                    var name = NameGenerator.GenerateUniqueName("unused",
                        n => !usedNames.Contains(n) &&
                             !localsInNestedScope.Contains(n) &&
                             semanticModel.LookupSymbols(node.SpanStart, name: n).IsEmpty);
                    usedNames.Add(name);
                    return name;
                }
                finally
                {
                    localsInNestedScope.Free();
                }
            }

            void AddLocalsInNestedScope(SyntaxNode node, PooledHashSet<string> localsInNestedScope)
            {
                var blockAncestor = node.FirstAncestorOrSelf<SyntaxNode>(n => syntaxFacts.IsExecutableBlock(n));
                if (blockAncestor != null)
                {
                    foreach (var variableDeclarator in blockAncestor.DescendantNodes().OfType<TVariableDeclaratorSyntax>())
                    {
                        var name = syntaxFacts.GetIdentifierOfVariableDeclarator(variableDeclarator).ValueText;
                        localsInNestedScope.Add(name);
                    }
                }
            }
        }

        private void FixAll(
            string diagnosticId,
            IOrderedEnumerable<Diagnostic> diagnostics,
            SemanticModel semanticModel,
            SyntaxNode root,
            UnusedValuePreference preference,
            bool removeAssignments,
            Func<SyntaxNode, string> generateUniqueNameAtSpanStart,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId:
                    FixAllExpressionValueIsUnusedDiagnostics(diagnostics, semanticModel, root,
                        preference, generateUniqueNameAtSpanStart, editor, syntaxFacts, cancellationToken);
                    break;

                case IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId:
                    FixAllValueAssignedIsUnusedDiagnostics(diagnostics, semanticModel, root,
                        preference, removeAssignments, generateUniqueNameAtSpanStart, editor, syntaxFacts, cancellationToken);
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private void FixAllExpressionValueIsUnusedDiagnostics(
            IOrderedEnumerable<Diagnostic> diagnostics,
            SemanticModel semanticModel,
            SyntaxNode root,
            UnusedValuePreference preference,
            Func<SyntaxNode, string> generateUniqueNameAtSpanStart,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var expressionStatement = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TExpressionStatementSyntax>();
                if (expressionStatement == null)
                {
                    continue;
                }

                var expression = syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);
                switch (preference)
                {
                    case UnusedValuePreference.DiscardVariable:
                        Debug.Assert(semanticModel.Language != LanguageNames.VisualBasic);
                        var discardAssignmentExpression = (TExpressionSyntax)editor.Generator.AssignmentStatement(
                                left: editor.Generator.IdentifierName(DiscardVariableName), right: expression.WithoutTrivia())
                            .WithTriviaFrom(expression)
                            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                        editor.ReplaceNode(expression, discardAssignmentExpression);
                        break;

                    case UnusedValuePreference.UnusedLocalVariable:
                        // Add Simplifier annotation so that 'var'/explicit type is correctly added based on user options.
                        var localDecl = editor.Generator.LocalDeclarationStatement(
                                name: generateUniqueNameAtSpanStart(expressionStatement), initializer: expression.WithoutLeadingTrivia())
                            .WithTriviaFrom(expressionStatement)
                            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                        editor.ReplaceNode(expressionStatement, localDecl);
                        break;
                }
            }
        }

        private void FixAllValueAssignedIsUnusedDiagnostics(
            IOrderedEnumerable<Diagnostic> diagnostics,
            SemanticModel semanticModel,
            SyntaxNode root,
            UnusedValuePreference preference,
            bool removeAssignments,
            Func<SyntaxNode, string> generateUniqueNameAtSpanStart,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            var nodeReplacementMap = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance();
            var nodesToRemove = PooledHashSet<SyntaxNode>.GetInstance();
            var candidateNodesForRemoval = PooledHashSet<TLocalDeclarationStatementSyntax>.GetInstance();

            try
            {
                var nodesToFix = GetNodesToFix();

                // Note this fixer only operates on code blocks which have no syntax errors (see "HasSyntaxErrors" usage in AbstractRemoveUnusedExpressionsDiagnosticAnalyzer).
                // Hence, we can assume that each node to fix is parented by a StatementSyntax node.
                foreach (var nodesByStatement in nodesToFix.GroupBy(n => n.node.FirstAncestorOrSelf<TStatementSyntax>()))
                {
                    var statement = nodesByStatement.Key;
                    foreach (var (node, isUnusedLocalAssignment) in nodesByStatement)
                    {
                        var declaredLocal = semanticModel.GetDeclaredSymbol(node, cancellationToken) as ILocalSymbol;
                        if (declaredLocal == null && node.Parent is TCatchStatementSyntax)
                        {
                            declaredLocal = semanticModel.GetDeclaredSymbol(node.Parent, cancellationToken) as ILocalSymbol;
                        }

                        string newLocalNameOpt = null;
                        if (removeAssignments)
                        {
                            // Removable constant assignment or initialization.
                            if (declaredLocal != null)
                            {
                                // Constant value initialization.
                                // For example, "int a = 0;"
                                var variableDeclarator = node.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                                Debug.Assert(variableDeclarator != null);
                                nodesToRemove.Add(variableDeclarator);

                                // Local declaration statement containing the declarator might be a candidate for removal if all its variables get marked for removal.
                                candidateNodesForRemoval.Add(variableDeclarator.GetAncestor<TLocalDeclarationStatementSyntax>());
                            }
                            else
                            {
                                // Constant value assignment or increment/decrement.
                                if (syntaxFacts.IsOperandOfIncrementOrDecrementExpression(node))
                                {
                                    // For example, C# increment operation "a++;"
                                    Debug.Assert(node.Parent.Parent is TExpressionStatementSyntax);
                                    nodesToRemove.Add(node.Parent.Parent);
                                }
                                else
                                {
                                    Debug.Assert(syntaxFacts.IsLeftSideOfAnyAssignment(node));

                                    if (node.Parent is TStatementSyntax)
                                    {
                                        // For example, VB simple assignment statement "a = 0"
                                        nodesToRemove.Add(node.Parent);
                                    }
                                    else if (node.Parent is TExpressionSyntax && node.Parent.Parent is TExpressionStatementSyntax)
                                    {
                                        // For example, C# simple assignment statement "a = 0;"
                                        nodesToRemove.Add(node.Parent.Parent);
                                    }
                                    else
                                    {
                                        nodeReplacementMap.Add(node.Parent, syntaxFacts.GetRightHandSideOfAssignment(node.Parent));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Non-constant value initialization/assignment.
                            newLocalNameOpt = preference == UnusedValuePreference.DiscardVariable ? DiscardVariableName : generateUniqueNameAtSpanStart(node);
                            var newNameToken = editor.Generator.Identifier(newLocalNameOpt);
                            var newNameNode = TryUpdateNameForFlaggedNode(node, newNameToken);
                            if (newNameNode == null)
                            {
                                continue;
                            }

                            if (syntaxFacts.IsLeftSideOfAnyAssignment(node) && !syntaxFacts.IsLeftSideOfAssignment(node))
                            {
                                // Compound assignment is changed to simple assignment.
                                nodeReplacementMap.Add(node.Parent, editor.Generator.AssignmentStatement(newNameNode, syntaxFacts.GetRightHandSideOfAssignment(node.Parent)));
                            }
                            else
                            {
                                nodeReplacementMap.Add(node, newNameNode);
                            }
                        }

                        if (declaredLocal != null)
                        {
                            // We have a dead initialization for a local declaration.
                            var declarationStatement = CreateLocalDeclarationStatement(declaredLocal.Type, declaredLocal.Name);
                            if (isUnusedLocalAssignment)
                            {
                                declarationStatement = declarationStatement.WithAdditionalAnnotations(s_unusedLocalDeclarationAnnotation);
                            }

                            InsertLocalDeclarationStatement(declarationStatement, node);
                        }
                        else
                        {
                            // We have a dead assignment to a local/parameter.
                            // If the assignment value is a non-constant expression, and user prefers unused local variables for unused value assignment,
                            // create a new local declaration for the unused local.
                            if (preference == UnusedValuePreference.UnusedLocalVariable && !removeAssignments)
                            {
                                var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                                Debug.Assert(type != null);
                                Debug.Assert(newLocalNameOpt != null);
                                var declarationStatement = CreateLocalDeclarationStatement(type, newLocalNameOpt);
                                InsertLocalDeclarationStatement(declarationStatement, node);
                            }
                        }
                    }
                }

                foreach (var localDeclarationStatement in candidateNodesForRemoval)
                {
                    if (ShouldRemoveStatement(localDeclarationStatement, out var variables))
                    {
                        nodesToRemove.Add(localDeclarationStatement);
                        nodesToRemove.RemoveRange(variables);
                    }
                }

                foreach (var node in nodesToRemove)
                {
                    editor.RemoveNode(node, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepLeadingTrivia);
                }

                foreach (var kvp in nodeReplacementMap)
                {
                    editor.ReplaceNode(kvp.Key, kvp.Value.WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            finally
            {
                nodeReplacementMap.Free();
                nodesToRemove.Free();
            }

            return;

            // Local functions.
            IEnumerable<(SyntaxNode node, bool isUnusedLocalAssignment)> GetNodesToFix()
            {
                foreach (var diagnostic in diagnostics)
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                    var isUnusedLocalAssignment = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsUnusedLocalDiagnostic(diagnostic);
                    yield return (node, isUnusedLocalAssignment);
                }
            }

            // Mark generated local declaration statement with:
            //  1. "s_newLocalDeclarationAnnotation" for post processing in "MoveNewLocalDeclarationsNearReference" below.
            //  2. Simplifier annotation so that 'var'/explicit type is correctly added based on user options.
            TLocalDeclarationStatementSyntax CreateLocalDeclarationStatement(ITypeSymbol type, string name)
                => (TLocalDeclarationStatementSyntax)editor.Generator.LocalDeclarationStatement(type, name)
                   .WithLeadingTrivia(editor.Generator.ElasticCarriageReturnLineFeed)
                   .WithAdditionalAnnotations(s_newLocalDeclarationStatementAnnotation, Simplifier.Annotation);

            void InsertLocalDeclarationStatement(TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode node)
            {
                var insertionNode = node.FirstAncestorOrSelf<SyntaxNode>(n => n.Parent is TSwitchCaseBlockSyntax ||
                                                                              syntaxFacts.IsExecutableBlock(n.Parent) &&
                                                                              !(n is TCatchStatementSyntax) &&
                                                                              !(n is TCatchBlockSyntax));
                if (insertionNode is TSwitchCaseLabelOrClauseSyntax)
                {
                    InsertAtStartOfSwitchCaseBlock(insertionNode.GetAncestor<TSwitchCaseBlockSyntax>(), editor, declarationStatement);
                }
                else
                {
                    Debug.Assert(insertionNode is TStatementSyntax);
                    editor.InsertBefore(insertionNode, declarationStatement);
                }
            }

            bool ShouldRemoveStatement(TLocalDeclarationStatementSyntax localDeclarationStatement, out SeparatedSyntaxList<SyntaxNode> variables)
            {
                Debug.Assert(removeAssignments);

                // We should remove the entire local declaration statement if all its variables are marked for removal.
                variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                foreach (var variable in variables)
                {
                    if (!nodesToRemove.Contains(variable))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private async Task<SyntaxNode> PostProcessDocumentAsync(
            Document document,
            SyntaxNode currentRoot,
            string diagnosticId,
            UnusedValuePreference preference,
            CancellationToken cancellationToken)
        {
            // If we added discard assignments, replace all discard variable declarations in
            // this method with discard assignments, i.e. "var _ = M();" is replaced with "_ = M();"
            // This is done to prevent compiler errors where the existing method has a discard
            // variable declaration at a line following the one we added a discard assignment in our fix.
            if (preference == UnusedValuePreference.DiscardVariable)
            {
                currentRoot = await PostProcessDocumentCoreAsync(
                    RemoveDiscardDeclarationsAsync, currentRoot, document, cancellationToken).ConfigureAwait(false);
            }

            // If we added new variable declaration statements, move these as close as possible to their
            // first reference site.
            if (NeedsToMoveNewLocalDeclarationsNearReference(diagnosticId))
            {
                currentRoot = await PostProcessDocumentCoreAsync(
                    MoveNewLocalDeclarationsNearReferenceAsync, currentRoot, document, cancellationToken).ConfigureAwait(false);
            }

            return currentRoot;
        }

        private static async Task<SyntaxNode> PostProcessDocumentCoreAsync(
            Func<SyntaxNode, Document, CancellationToken, Task<SyntaxNode>> processMemberDeclarationAsync,
            SyntaxNode currentRoot,
            Document document,
            CancellationToken cancellationToken)
        {
            var newDocument = document.WithSyntaxRoot(currentRoot);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var memberDeclReplacementsMap = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance();
            try
            {
                foreach (var memberDecl in newRoot.DescendantNodes().Where(n => n.HasAnnotation(s_memberAnnotation)))
                {
                    var newMemberDecl = await processMemberDeclarationAsync(memberDecl, newDocument, cancellationToken).ConfigureAwait(false);
                    memberDeclReplacementsMap.Add(memberDecl, newMemberDecl);
                }

                return newRoot.ReplaceNodes(memberDeclReplacementsMap.Keys,
                    computeReplacementNode: (node, _) => memberDeclReplacementsMap[node]);
            }
            finally
            {
                memberDeclReplacementsMap.Free();
            }
        }

        /// <summary>
        /// Returns an updated <paramref name="memberDeclaration"/> with all the
        /// local declarations named '_' converted to simple assignments to discard.
        /// For example, <code>int _ = Computation();</code> is converted to
        /// <code>_ = Computation();</code>.
        /// This is needed to prevent the code fix/FixAll from generating code with
        /// multiple local variables named '_', which is a compiler error.
        /// </summary>
        protected abstract Task<SyntaxNode> RemoveDiscardDeclarationsAsync(
            SyntaxNode memberDeclaration,
            Document document,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns an updated <paramref name="memberDeclaration"/> with all the
        /// local declaration statements annotated with <see cref="s_newLocalDeclarationStatementAnnotation"/>
        /// moved closer to first reference.
        /// </summary>
        private async Task<SyntaxNode> MoveNewLocalDeclarationsNearReferenceAsync(
            SyntaxNode memberDeclaration,
            Document document,
            CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IMoveDeclarationNearReferenceService>();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var originalDeclStatementsToMove = memberDeclaration.DescendantNodes()
                                                                .Where(n => n.HasAnnotation(s_newLocalDeclarationStatementAnnotation))
                                                                .ToImmutableArray();
            if (originalDeclStatementsToMove.IsEmpty)
            {
                return memberDeclaration;
            }

            // Moving declarations closer to a reference can lead to conflicting edits.
            // So, we track all the declaration statements to be moved upfront, and update
            // the root, document, editor and memberDeclaration for every edit.
            // Finally, we apply replace the memberDeclaration in the originalEditor as a single edit.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rootWithTrackedNodes = root.TrackNodes(originalDeclStatementsToMove);

            // Run formatter prior to invoking IMoveDeclarationNearReferenceService.
            rootWithTrackedNodes = Formatter.Format(rootWithTrackedNodes, originalDeclStatementsToMove.Select(s => s.Span), document.Project.Solution.Workspace, cancellationToken: cancellationToken);

            document = document.WithSyntaxRoot(rootWithTrackedNodes);
            await OnDocumentUpdatedAsync().ConfigureAwait(false);

            foreach (TLocalDeclarationStatementSyntax originalDeclStatement in originalDeclStatementsToMove)
            {
                // Get the current declaration statement.
                var declStatement = memberDeclaration.GetCurrentNode(originalDeclStatement);

                var documentUpdated = false;

                // Check if the new variable declaration is unused after all the fixes, and hence can be removed.
                if (await TryRemoveUnusedLocalAsync(declStatement).ConfigureAwait(false))
                {
                    documentUpdated = true;
                }
                else
                {
                    // Otherwise, move the declaration closer to the first reference if possible.
                    if (await service.CanMoveDeclarationNearReferenceAsync(document, declStatement, canMovePastOtherDeclarationStatements: true, cancellationToken).ConfigureAwait(false))
                    {
                        document = await service.MoveDeclarationNearReferenceAsync(document, declStatement, cancellationToken).ConfigureAwait(false);
                        documentUpdated = true;
                    }
                }

                if (documentUpdated)
                {
                    await OnDocumentUpdatedAsync().ConfigureAwait(false);
                }
            }

            return memberDeclaration;

            // Local functions.
            async Task OnDocumentUpdatedAsync()
            {
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                memberDeclaration = syntaxFacts.GetContainingMemberDeclaration(root, memberDeclaration.SpanStart);
            }

            async Task<bool> TryRemoveUnusedLocalAsync(TLocalDeclarationStatementSyntax newDecl)
            {
                if (newDecl.HasAnnotation(s_unusedLocalDeclarationAnnotation))
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var localDeclarationOperation = semanticModel.GetOperation(newDecl, cancellationToken) as IVariableDeclarationGroupOperation;
                    var local = localDeclarationOperation?.GetDeclaredVariables().Single();

                    var referencedSymbols = await SymbolFinder.FindReferencesAsync(local, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                    if (referencedSymbols.Count() == 1 &&
                        referencedSymbols.Single().Locations.IsEmpty())
                    {
                        document = document.WithSyntaxRoot(
                            root.RemoveNode(newDecl, SyntaxGenerator.DefaultRemoveOptions));
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey) :
                base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
