// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReplaceDiscardDeclarationsWithAssignments;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        where TForEachStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
        where TSwitchCaseBlockSyntax : SyntaxNode
        where TSwitchCaseLabelOrClauseSyntax : SyntaxNode
    {
        private static readonly SyntaxAnnotation s_memberAnnotation = new();
        private static readonly SyntaxAnnotation s_newLocalDeclarationStatementAnnotation = new();
        private static readonly SyntaxAnnotation s_unusedLocalDeclarationAnnotation = new();
        private static readonly SyntaxAnnotation s_existingLocalDeclarationWithoutInitializerAnnotation = new();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                                     IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId);

        protected abstract ISyntaxFormatting GetSyntaxFormatting();

        /// <summary>
        /// Method to update the identifier token for the local/parameter declaration or reference
        /// that was flagged as an unused value write by the analyzer.
        /// Returns null if the provided node is not one of the handled node kinds.
        /// Otherwise, returns the new node with updated identifier.
        /// </summary>
        /// <param name="node">Flagged node containing the identifier token to be replaced.</param>
        /// <param name="newName">New identifier token</param>
        protected abstract SyntaxNode TryUpdateNameForFlaggedNode(SyntaxNode node, SyntaxToken newName);

        /// <summary>
        /// Gets the identifier token for the iteration variable of the given foreach statement node.
        /// </summary>
        protected abstract SyntaxToken GetForEachStatementIdentifier(TForEachStatementSyntax node);

        /// <summary>
        /// Wraps the given statements within a block statement.
        /// Note this method is invoked when replacing a statement that is parented by a non-block statement syntax.
        /// </summary>
        protected abstract TBlockSyntax WrapWithBlockIfNecessary(IEnumerable<TStatementSyntax> statements);

        /// <summary>
        /// Inserts the given declaration statement at the start of the given switch case block.
        /// </summary>
        protected abstract void InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(TSwitchCaseBlockSyntax switchCaseBlock, SyntaxEditor editor, TLocalDeclarationStatementSyntax declarationStatement);

        /// <summary>
        /// Gets the replacement node for a compound assignment expression whose
        /// assigned value is redundant.
        /// For example, "x += MethodCall()", where assignment to 'x' is redundant
        /// is replaced with "_ = MethodCall()" or "var unused = MethodCall()"
        /// </summary>
        protected abstract SyntaxNode GetReplacementNodeForCompoundAssignment(
            SyntaxNode originalCompoundAssignment,
            SyntaxNode newAssignmentTarget,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts);

        /// <summary>
        /// Gets the replacement node for a var pattern.
        /// We need just to change the identifier of the pattern, not the whole node
        /// </summary>
        protected abstract SyntaxNode GetReplacementNodeForVarPattern(SyntaxNode originalVarPattern, SyntaxNode newNameNode);

        /// <summary>
        /// Rewrite the parent of a node which was rewritten by <see cref="TryUpdateNameForFlaggedNode"/>.
        /// </summary>
        /// <param name="parent">The original parent of the node rewritten by <see cref="TryUpdateNameForFlaggedNode"/>.</param>
        /// <param name="newNameNode">The rewritten node produced by <see cref="TryUpdateNameForFlaggedNode"/>.</param>
        /// <param name="editor">The syntax editor for the code fix.</param>
        /// <param name="syntaxFacts">The syntax facts for the current language.</param>
        /// <param name="semanticModel">Semantic model for the tree.</param>
        /// <returns>The replacement node to use in the rewritten syntax tree; otherwise, <see langword="null"/> to only
        /// rewrite the node originally rewritten by <see cref="TryUpdateNameForFlaggedNode"/>.</returns>
        protected virtual SyntaxNode? TryUpdateParentOfUpdatedNode(SyntaxNode parent, SyntaxNode newNameNode, SyntaxEditor editor, ISyntaxFacts syntaxFacts, SemanticModel semanticModel) => null;

        /// <summary>
        /// Computes correct replacement node, including cases with recursive changes (e.g. recursive pattern node rewrite in fix-all scenario)
        /// </summary>
        /// <param name="originalOldNode">The original node for replacement</param>
        /// <param name="changedOldNode">Node for replacement transformed by previous replacements</param>
        /// <param name="proposedReplacementNode">Proposed replacement node with changes relative to <paramref name="originalOldNode"/></param>
        /// <returns>The final replacement for the node</returns>
        protected abstract SyntaxNode ComputeReplacementNode(SyntaxNode originalOldNode, SyntaxNode changedOldNode, SyntaxNode proposedReplacementNode);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            if (!AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.TryGetUnusedValuePreference(diagnostic, out var preference))
            {
                return;
            }

            var isRemovableAssignment = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostic);

            string title;
            if (isRemovableAssignment)
            {
                // Recommend removing the redundant constant value assignment.
                title = CodeFixesResources.Remove_redundant_assignment;
            }
            else
            {
                // Recommend using discard/unused local for redundant non-constant assignment.
                switch (preference)
                {
                    case UnusedValuePreference.DiscardVariable:
                        if (IsForEachIterationVariableDiagnostic(diagnostic, context.Document, context.CancellationToken))
                        {
                            // Do not offer a fix to replace unused foreach iteration variable with discard.
                            // User should probably replace it with a for loop based on the collection length.
                            return;
                        }

                        title = CodeFixesResources.Use_discard_underscore;

                        var syntaxFacts = context.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                        var node = root.FindNode(context.Span, getInnermostNodeForTie: true);

                        // Check if this is compound assignment which is not parented by an expression statement,
                        // for example "return x += M();" OR "=> x ??= new C();"
                        // If so, we will be replacing this compound assignment with the underlying binary operation.
                        // For the above examples, it will be "return x + M();" AND "=> x ?? new C();" respectively.
                        // For these cases, we want to show the title as "Remove redundant assignment" instead of "Use discard _".
                        if (syntaxFacts.IsLeftSideOfCompoundAssignment(node) &&
                            !syntaxFacts.IsExpressionStatement(node.Parent))
                        {
                            title = CodeFixesResources.Remove_redundant_assignment;
                        }
                        // Also we want to show "Remove redundant assignment" title for variable designation in pattern matching,
                        // since this assignment will be fully removed. Cases:
                        // 1) `if (obj is SomeType someType)`
                        // 2) `if (obj is { } someType)`
                        // 3) `if (obj is [] someType)`
                        else if (syntaxFacts.IsDeclarationPattern(node.Parent) ||
                                 syntaxFacts.IsRecursivePattern(node.Parent) ||
                                 syntaxFacts.IsListPattern(node.Parent))
                        {
                            title = CodeFixesResources.Remove_redundant_assignment;
                        }

                        break;

                    case UnusedValuePreference.UnusedLocalVariable:
                        title = CodeFixesResources.Use_discarded_local;
                        break;

                    default:
                        return;
                }
            }

            RegisterCodeFix(context, title, GetEquivalenceKey(preference, isRemovableAssignment));
        }

        private static bool IsForEachIterationVariableDiagnostic(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            // Do not offer a fix to replace unused foreach iteration variable with discard.
            // User should probably replace it with a for loop based on the collection length.
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return syntaxFacts.IsForEachStatement(diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken));
        }

        private static string GetEquivalenceKey(UnusedValuePreference preference, bool isRemovableAssignment)
            => preference.ToString() + isRemovableAssignment;

        private static string GetEquivalenceKey(Diagnostic diagnostic)
        {
            if (!AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.TryGetUnusedValuePreference(diagnostic, out var preference))
            {
                return string.Empty;
            }

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

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
        {
            return equivalenceKey == GetEquivalenceKey(diagnostic) &&
                !IsForEachIterationVariableDiagnostic(diagnostic, document, cancellationToken);
        }

        private static IEnumerable<IGrouping<SyntaxNode, Diagnostic>> GetDiagnosticsGroupedByMember(
            ImmutableArray<Diagnostic> diagnostics,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode root,
            out string diagnosticId,
            out UnusedValuePreference preference,
            out bool removeAssignments)
        {
            diagnosticId = diagnostics[0].Id;
            var success = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.TryGetUnusedValuePreference(diagnostics[0], out preference);
            Debug.Assert(success);
            removeAssignments = AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostics[0]);
#if DEBUG
            foreach (var diagnostic in diagnostics)
            {
                Debug.Assert(diagnosticId == diagnostic.Id);
                Debug.Assert(AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.TryGetUnusedValuePreference(diagnostic, out var diagnosticPreference) &&
                             diagnosticPreference == preference);
                Debug.Assert(removeAssignments == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.GetIsRemovableAssignmentDiagnostic(diagnostic));
            }
#endif

            return GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root);
        }

        private static IEnumerable<IGrouping<SyntaxNode, Diagnostic>> GetDiagnosticsGroupedByMember(
            ImmutableArray<Diagnostic> diagnostics,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode root)
        {
            return diagnostics.GroupBy(d => syntaxFacts.GetContainingMemberDeclaration(root, d.Location.SourceSpan.Start) ?? root);
        }

        private static async Task<Document> PreprocessDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            // Track all the member declaration nodes that have diagnostics.
            // We will post process all these tracked nodes after applying the fix (see "PostProcessDocumentAsync" below in this source file).

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var memberDeclarations = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root).Select(g => g.Key);
            root = root.ReplaceNodes(memberDeclarations, computeReplacementNode: (_, n) => n.WithAdditionalAnnotations(s_memberAnnotation));
            return document.WithSyntaxRoot(root);
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var options = await document.GetCodeFixOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var formattingOptions = options.GetFormattingOptions(GetSyntaxFormatting());
            var preprocessedDocument = await PreprocessDocumentAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            var newRoot = await GetNewRootAsync(preprocessedDocument, formattingOptions, diagnostics, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(editor.OriginalRoot, newRoot);
        }

        private async Task<SyntaxNode> GetNewRootAsync(
            Document document,
            SyntaxFormattingOptions options,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);

            // We compute the code fix in two passes:
            //   1. The first pass groups the diagnostics to fix by containing member declaration and
            //      computes and applies the core code fixes. Grouping is done to ensure we choose
            //      the most appropriate name for new unused local declarations, which can clash
            //      with existing local declarations in the method body.
            //   2. Second pass (PostProcessDocumentAsync) performs additional syntax manipulations
            //      for the fixes produced from the first pass:
            //      a. Replace discard declarations, such as "var _ = M();" that conflict with newly added
            //         discard assignments, with discard assignments of the form "_ = M();"
            //      b. Move newly introduced local declaration statements closer to the local variable's
            //         first reference.

            // Get diagnostics grouped by member.
            var diagnosticsGroupedByMember = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root,
                out var diagnosticId, out var preference, out var removeAssignments);

            // First pass to compute and apply the core code fixes.
            foreach (var diagnosticsToFix in diagnosticsGroupedByMember)
            {
                var containingMemberDeclaration = diagnosticsToFix.Key;
                using var nameGenerator = new UniqueVariableNameGenerator(containingMemberDeclaration, semanticModel, semanticFacts, cancellationToken);

                await FixAllAsync(
                    diagnosticId, diagnosticsToFix.Select(d => d),
                    document, semanticModel, root, containingMemberDeclaration, preference,
                    removeAssignments, nameGenerator, editor, cancellationToken).ConfigureAwait(false);
            }

            // Second pass to post process the document.
            var currentRoot = editor.GetChangedRoot();
            var newRoot = await PostProcessDocumentAsync(document, options, currentRoot,
                diagnosticId, preference, cancellationToken).ConfigureAwait(false);

            if (currentRoot != newRoot)
                editor.ReplaceNode(root, newRoot);

            return editor.GetChangedRoot();
        }

        private async Task FixAllAsync(
            string diagnosticId,
            IEnumerable<Diagnostic> diagnostics,
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            SyntaxNode containingMemberDeclaration,
            UnusedValuePreference preference,
            bool removeAssignments,
            UniqueVariableNameGenerator nameGenerator,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId:
                    // Make sure the inner diagnostics are placed first
                    FixAllExpressionValueIsUnusedDiagnostics(
                        diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start),
                        document, semanticModel, root, preference, nameGenerator, editor);
                    break;

                case IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId:
                    // Make sure the diagnostics are placed in order.
                    // Example: 
                    // int a = 0; int b = 1;
                    // After fix it would be int a; int b;
                    await FixAllValueAssignedIsUnusedDiagnosticsAsync(
                        diagnostics.OrderBy(d => d.Location.SourceSpan.Start),
                        document, semanticModel, root, containingMemberDeclaration,
                        preference, removeAssignments, nameGenerator, editor, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        private static void FixAllExpressionValueIsUnusedDiagnostics(
            IOrderedEnumerable<Diagnostic> diagnostics,
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            UnusedValuePreference preference,
            UniqueVariableNameGenerator nameGenerator,
            SyntaxEditor editor)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // This method applies the code fix for diagnostics reported for expression statement dropping values.
            // We replace each flagged expression statement with an assignment to a discard variable or a new unused local,
            // based on the user's preference.
            // Note: The diagnostic order here should be inner first and outer second.
            // Example: Foo1(() => { Foo2(); })
            // Foo2() should be the first in this case.
            foreach (var diagnostic in diagnostics)
            {
                var expressionStatement = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TExpressionStatementSyntax>();
                if (expressionStatement == null)
                {
                    continue;
                }

                switch (preference)
                {
                    case UnusedValuePreference.DiscardVariable:
                        Debug.Assert(semanticModel.Language != LanguageNames.VisualBasic);
                        var expression = syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);
                        editor.ReplaceNode(expression, (node, generator) =>
                        {
                            var discardAssignmentExpression = (TExpressionSyntax)generator.AssignmentStatement(
                                                                    left: generator.IdentifierName(AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName),
                                                                    right: node.WithoutTrivia())
                                                                .WithTriviaFrom(node)
                                                                .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                            return discardAssignmentExpression;
                        });
                        break;

                    case UnusedValuePreference.UnusedLocalVariable:
                        var name = nameGenerator.GenerateUniqueNameAtSpanStart(expressionStatement).ValueText;
                        editor.ReplaceNode(expressionStatement, (node, generator) =>
                        {
                            var expression = syntaxFacts.GetExpressionOfExpressionStatement(node);
                            // Add Simplifier annotation so that 'var'/explicit type is correctly added based on user options.
                            var localDecl = editor.Generator.LocalDeclarationStatement(
                                                name: name,
                                                initializer: expression.WithoutLeadingTrivia())
                                            .WithTriviaFrom(node)
                                            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                            return localDecl;
                        });
                        break;
                }
            }
        }

        private async Task FixAllValueAssignedIsUnusedDiagnosticsAsync(
            IOrderedEnumerable<Diagnostic> diagnostics,
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            SyntaxNode containingMemberDeclaration,
            UnusedValuePreference preference,
            bool removeAssignments,
            UniqueVariableNameGenerator nameGenerator,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var blockFacts = document.GetRequiredLanguageService<IBlockFactsService>();

            // This method applies the code fix for diagnostics reported for unused value assignments to local/parameter.
            // The actual code fix depends on whether or not the right hand side of the assignment has side effects.
            // For example, if the right hand side is a constant or a reference to a local/parameter, then it has no side effects.
            // The lack of side effects is indicated by the "removeAssignments" parameter for this function.

            // If the right hand side has no side effects, then we can replace the assignments with variable declarations that have no initializer
            // or completely remove the statement.
            // If the right hand side does have side effects, we replace the identifier token for unused value assignment with
            // a new identifier token (either discard '_' or new unused local variable name).

            // For both the above cases, if the original diagnostic was reported on a local declaration, i.e. redundant initialization
            // at declaration, then we also add a new variable declaration statement without initializer for this local.

            using var _1 = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance(out var nodeReplacementMap);
            using var _2 = PooledHashSet<SyntaxNode>.GetInstance(out var nodesToRemove);
            using var _3 = PooledHashSet<(TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode node)>.GetInstance(out var nodesToAdd);
            // Indicates if the node's trivia was processed.
            using var _4 = PooledHashSet<SyntaxNode>.GetInstance(out var processedNodes);
            using var _5 = PooledHashSet<TLocalDeclarationStatementSyntax>.GetInstance(out var candidateDeclarationStatementsForRemoval);
            var hasAnyUnusedLocalAssignment = false;

            foreach (var (node, isUnusedLocalAssignment) in GetNodesToFix())
            {
                hasAnyUnusedLocalAssignment |= isUnusedLocalAssignment;

                var declaredLocal = semanticModel.GetDeclaredSymbol(node, cancellationToken) as ILocalSymbol;
                if (declaredLocal == null && node.Parent is TCatchStatementSyntax)
                {
                    declaredLocal = semanticModel.GetDeclaredSymbol(node.Parent, cancellationToken) as ILocalSymbol;
                }

                string? newLocalNameOpt = null;
                if (removeAssignments)
                {
                    // Removable assignment or initialization, such that right hand side has no side effects.
                    if (declaredLocal != null)
                    {
                        // Redundant initialization.
                        // For example, "int a = 0;"
                        var variableDeclarator = node.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                        Contract.ThrowIfNull(variableDeclarator);
                        nodesToRemove.Add(variableDeclarator);

                        // Local declaration statement containing the declarator might be a candidate for removal if all its variables get marked for removal.
                        var candidate = GetCandidateLocalDeclarationForRemoval(variableDeclarator);
                        if (candidate != null)
                        {
                            candidateDeclarationStatementsForRemoval.Add(candidate);
                        }
                    }
                    else
                    {
                        // Redundant assignment or increment/decrement.
                        if (syntaxFacts.IsOperandOfIncrementOrDecrementExpression(node))
                        {
                            // For example, C# increment operation "a++;"
                            Contract.ThrowIfFalse(node.GetRequiredParent().Parent is TExpressionStatementSyntax);
                            nodesToRemove.Add(node.GetRequiredParent().GetRequiredParent());
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
                                // For example, C# nested assignment statement "a = b = 0;", where assignment to 'b' is redundant.
                                // We replace the node with "a = 0;"
                                nodeReplacementMap.Add(node.GetRequiredParent(), syntaxFacts.GetRightHandSideOfAssignment(node.GetRequiredParent()));
                            }
                        }
                    }
                }
                else
                {
                    // Value initialization/assignment where the right hand side may have side effects,
                    // and hence needs to be preserved in fixed code.
                    // For example, "x = MethodCall();" is replaced with "_ = MethodCall();" or "var unused = MethodCall();"

                    // Replace the flagged variable's identifier token with new named, based on user's preference.
                    var newNameToken = preference == UnusedValuePreference.DiscardVariable
                        ? document.GetRequiredLanguageService<SyntaxGeneratorInternal>().Identifier(AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName)
                        : nameGenerator.GenerateUniqueNameAtSpanStart(node);
                    newLocalNameOpt = newNameToken.ValueText;
                    var newNameNode = TryUpdateNameForFlaggedNode(node, newNameToken);
                    if (newNameNode == null)
                    {
                        continue;
                    }

                    // Is this is compound assignment?
                    if (syntaxFacts.IsLeftSideOfAnyAssignment(node) && !syntaxFacts.IsLeftSideOfAssignment(node))
                    {
                        // Compound assignment is changed to simple assignment.
                        // For example, "x += MethodCall();", where assignment to 'x' is redundant
                        // is replaced with "_ = MethodCall();" or "var unused = MethodCall();"
                        nodeReplacementMap.Add(node.GetRequiredParent(), GetReplacementNodeForCompoundAssignment(node.GetRequiredParent(), newNameNode, editor, syntaxFacts));
                    }
                    else if (syntaxFacts.IsVarPattern(node))
                    {
                        nodeReplacementMap.Add(node, GetReplacementNodeForVarPattern(node, newNameNode));
                    }
                    else
                    {
                        var newParentNode = TryUpdateParentOfUpdatedNode(node.GetRequiredParent(), newNameNode, editor, syntaxFacts, semanticModel);
                        if (newParentNode is not null)
                        {
                            nodeReplacementMap.Add(node.GetRequiredParent(), newParentNode);
                        }
                        else
                        {
                            nodeReplacementMap.Add(node, newNameNode);
                        }
                    }
                }

                if (declaredLocal != null)
                {
                    // We have a dead initialization for a local declaration.
                    // Introduce a new local declaration statement without an initializer for this local.
                    var declarationStatement = CreateLocalDeclarationStatement(declaredLocal.Type, declaredLocal.Name);
                    if (isUnusedLocalAssignment)
                    {
                        declarationStatement = declarationStatement.WithAdditionalAnnotations(s_unusedLocalDeclarationAnnotation);
                    }

                    nodesToAdd.Add((declarationStatement, node));
                }
                else
                {
                    // We have a dead assignment to a local/parameter, which is not at the declaration site.
                    // Create a new local declaration for the unused local if both following conditions are met:
                    //  1. User prefers unused local variables for unused value assignment AND
                    //  2. Assignment value has side effects and hence cannot be removed.
                    if (preference == UnusedValuePreference.UnusedLocalVariable && !removeAssignments)
                    {
                        var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                        Contract.ThrowIfNull(type);
                        Contract.ThrowIfNull(newLocalNameOpt);
                        var declarationStatement = CreateLocalDeclarationStatement(type, newLocalNameOpt);
                        nodesToAdd.Add((declarationStatement, node));
                    }
                }
            }

            // Process candidate declaration statements for removal.
            foreach (var localDeclarationStatement in candidateDeclarationStatementsForRemoval)
            {
                // If all the variable declarators for the local declaration statement are being removed,
                // we can remove the entire local declaration statement.
                if (ShouldRemoveStatement(localDeclarationStatement, out var variables))
                {
                    nodesToRemove.Add(localDeclarationStatement);
                    nodesToRemove.RemoveRange(variables);
                }
            }

            foreach (var (declarationStatement, node) in nodesToAdd)
            {
                InsertLocalDeclarationStatement(declarationStatement, node);
            }

            if (hasAnyUnusedLocalAssignment)
            {
                // Local declaration statements with no initializer, but non-zero references are candidates for removal
                // if the code fix removes all these references.
                // We annotate such declaration statements with no initializer and non-zero references here
                // and remove them in post process document pass later, if the code fix did remove all these references.
                foreach (var localDeclarationStatement in containingMemberDeclaration.DescendantNodes().OfType<TLocalDeclarationStatementSyntax>())
                {
                    var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                    if (variables.Count == 1 &&
                        syntaxFacts.GetInitializerOfVariableDeclarator(variables[0]) == null &&
                        !(await IsLocalDeclarationWithNoReferencesAsync(localDeclarationStatement, document, cancellationToken).ConfigureAwait(false)))
                    {
                        nodeReplacementMap.Add(localDeclarationStatement, localDeclarationStatement.WithAdditionalAnnotations(s_existingLocalDeclarationWithoutInitializerAnnotation));
                    }
                }
            }

            foreach (var node in nodesToRemove)
            {
                var removeOptions = SyntaxGenerator.DefaultRemoveOptions;
                // If the leading trivia was not added to a new node, process it now.
                if (!processedNodes.Contains(node))
                {
                    // Don't keep trivia if the node is part of a multiple declaration statement.
                    // e.g. int x = 0, y = 0, z = 0; any white space left behind can cause problems if the declaration gets split apart.
                    var containingDeclaration = node.GetAncestor<TLocalDeclarationStatementSyntax>();
                    if (containingDeclaration != null && candidateDeclarationStatementsForRemoval.Contains(containingDeclaration))
                    {
                        removeOptions = SyntaxRemoveOptions.KeepNoTrivia;
                    }
                    else
                    {
                        removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
                    }
                }

                editor.RemoveNode(node, removeOptions);
            }

            foreach (var (node, replacement) in nodeReplacementMap)
                editor.ReplaceNode(node, (oldNode, _) => ComputeReplacementNode(node, oldNode, replacement));

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
                   .WithLeadingTrivia(syntaxFacts.ElasticCarriageReturnLineFeed)
                   .WithAdditionalAnnotations(s_newLocalDeclarationStatementAnnotation, Simplifier.Annotation);

            void InsertLocalDeclarationStatement(TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode node)
            {
                // Find the correct place to insert the given declaration statement based on the node's ancestors.
                var insertionNode = node.FirstAncestorOrSelf<SyntaxNode>(
                    n => n.Parent is TSwitchCaseBlockSyntax ||
                         blockFacts.IsExecutableBlock(n.Parent) &&
                         n is not TCatchStatementSyntax &&
                         n is not TCatchBlockSyntax);
                if (insertionNode is TSwitchCaseLabelOrClauseSyntax)
                {
                    InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(
                        insertionNode.GetAncestor<TSwitchCaseBlockSyntax>()!, editor, declarationStatement);
                }
                else if (insertionNode is TStatementSyntax)
                {
                    // If the insertion node is being removed, keep the leading trivia (following any directives) with
                    // the new declaration.
                    if (nodesToRemove.Contains(insertionNode) && !processedNodes.Contains(insertionNode))
                    {
                        // Fix 48070 - The Leading Trivia of the insertion node needs to be filtered
                        // to only include trivia after Directives (if there are any)
                        var leadingTrivia = insertionNode.GetLeadingTrivia();
                        var lastDirective = leadingTrivia.LastOrDefault(t => t.IsDirective);
                        var lastDirectiveIndex = leadingTrivia.IndexOf(lastDirective);
                        declarationStatement = declarationStatement.WithLeadingTrivia(leadingTrivia.Skip(lastDirectiveIndex + 1));

                        // Mark the node as processed so that the trivia only gets added once.
                        processedNodes.Add(insertionNode);
                    }

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

        protected abstract TLocalDeclarationStatementSyntax GetCandidateLocalDeclarationForRemoval(TVariableDeclaratorSyntax declarator);

        private async Task<SyntaxNode> PostProcessDocumentAsync(
            Document document,
            SyntaxFormattingOptions options,
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
                    ReplaceDiscardDeclarationsWithAssignmentsAsync, currentRoot, document, options, cancellationToken).ConfigureAwait(false);
            }

            // If we added new variable declaration statements, move these as close as possible to their
            // first reference site.
            if (NeedsToMoveNewLocalDeclarationsNearReference(diagnosticId))
            {
                currentRoot = await PostProcessDocumentCoreAsync(
                    AdjustLocalDeclarationsAsync, currentRoot, document, options, cancellationToken).ConfigureAwait(false);
            }

            return currentRoot;
        }

        private static async Task<SyntaxNode> PostProcessDocumentCoreAsync(
            Func<SyntaxNode, Document, SyntaxFormattingOptions, CancellationToken, Task<SyntaxNode>> processMemberDeclarationAsync,
            SyntaxNode currentRoot,
            Document document,
            SyntaxFormattingOptions options,
            CancellationToken cancellationToken)
        {
            // Process each member declaration which had at least one diagnostic reported in the original tree and hence
            // was annotated with "s_memberAnnotation" for post processing.

            var newDocument = document.WithSyntaxRoot(currentRoot);
            var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _1 = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance(out var memberDeclReplacementsMap);

            foreach (var memberDecl in newRoot.DescendantNodes().Where(n => n.HasAnnotation(s_memberAnnotation)))
            {
                var newMemberDecl = await processMemberDeclarationAsync(memberDecl, newDocument, options, cancellationToken).ConfigureAwait(false);
                memberDeclReplacementsMap.Add(memberDecl, newMemberDecl);
            }

            return newRoot.ReplaceNodes(memberDeclReplacementsMap.Keys,
                computeReplacementNode: (node, _) => memberDeclReplacementsMap[node]);
        }

        /// <summary>
        /// Returns an updated <paramref name="memberDeclaration"/> with all the
        /// local declarations named '_' converted to simple assignments to discard.
        /// For example, <code>int _ = Computation();</code> is converted to
        /// <code>_ = Computation();</code>.
        /// This is needed to prevent the code fix/FixAll from generating code with
        /// multiple local variables named '_', which is a compiler error.
        /// </summary>
        private async Task<SyntaxNode> ReplaceDiscardDeclarationsWithAssignmentsAsync(SyntaxNode memberDeclaration, Document document, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IReplaceDiscardDeclarationsWithAssignmentsService>();
            if (service == null)
                return memberDeclaration;

            return await service.ReplaceAsync(document, memberDeclaration, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an updated <paramref name="memberDeclaration"/> with all the new
        /// local declaration statements annotated with <see cref="s_newLocalDeclarationStatementAnnotation"/>
        /// moved closer to first reference and all the existing
        /// local declaration statements annotated with <see cref="s_existingLocalDeclarationWithoutInitializerAnnotation"/>
        /// whose declared local is no longer used removed.
        /// </summary>
        private async Task<SyntaxNode> AdjustLocalDeclarationsAsync(
            SyntaxNode memberDeclaration,
            Document document,
            SyntaxFormattingOptions options,
            CancellationToken cancellationToken)
        {
            var moveDeclarationService = document.GetRequiredLanguageService<IMoveDeclarationNearReferenceService>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalDocument = document;
            var originalDeclStatementsToMoveOrRemove =
                memberDeclaration.DescendantNodes()
                                 .Where(n => n.HasAnnotation(s_newLocalDeclarationStatementAnnotation) ||
                                             n.HasAnnotation(s_existingLocalDeclarationWithoutInitializerAnnotation))
                                 .ToImmutableArray();
            if (originalDeclStatementsToMoveOrRemove.IsEmpty)
            {
                return memberDeclaration;
            }

            // Moving declarations closer to a reference can lead to conflicting edits.
            // So, we track all the declaration statements to be moved upfront, and update
            // the root, document, editor and memberDeclaration for every edit.
            // Finally, we apply replace the memberDeclaration in the originalEditor as a single edit.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rootWithTrackedNodes = root.TrackNodes(originalDeclStatementsToMoveOrRemove);

            // Run formatter prior to invoking IMoveDeclarationNearReferenceService.
#if CODE_STYLE
            var provider = GetSyntaxFormatting();
            rootWithTrackedNodes = FormatterHelper.Format(rootWithTrackedNodes, originalDeclStatementsToMoveOrRemove.Select(s => s.Span), provider, options, rules: null, cancellationToken);
#else
            var provider = document.Project.Solution.Services;
            rootWithTrackedNodes = Formatter.Format(rootWithTrackedNodes, originalDeclStatementsToMoveOrRemove.Select(s => s.Span), provider, options, rules: null, cancellationToken);
#endif

            document = document.WithSyntaxRoot(rootWithTrackedNodes);
            await OnDocumentUpdatedAsync().ConfigureAwait(false);

            foreach (TLocalDeclarationStatementSyntax originalDeclStatement in originalDeclStatementsToMoveOrRemove)
            {
                // Get the current declaration statement.
                var declStatement = memberDeclaration.GetCurrentNode(originalDeclStatement);
                Contract.ThrowIfNull(declStatement);

                // Check if the variable declaration is unused after all the fixes, and hence can be removed.
                if (await TryRemoveUnusedLocalAsync(declStatement, originalDeclStatement).ConfigureAwait(false))
                {
                    await OnDocumentUpdatedAsync().ConfigureAwait(false);
                }
                else if (declStatement.HasAnnotation(s_newLocalDeclarationStatementAnnotation))
                {
                    // Otherwise, move the declaration closer to the first reference if possible.
                    if (await moveDeclarationService.CanMoveDeclarationNearReferenceAsync(document, declStatement, cancellationToken).ConfigureAwait(false))
                    {
                        document = await moveDeclarationService.MoveDeclarationNearReferenceAsync(document, declStatement, cancellationToken).ConfigureAwait(false);
                        await OnDocumentUpdatedAsync().ConfigureAwait(false);
                    }
                }
            }

            return memberDeclaration;

            // Local functions.
            async Task OnDocumentUpdatedAsync()
            {
                root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                memberDeclaration = syntaxFacts.GetContainingMemberDeclaration(root, memberDeclaration.SpanStart) ?? root;
            }

            async Task<bool> TryRemoveUnusedLocalAsync(TLocalDeclarationStatementSyntax newDecl, TLocalDeclarationStatementSyntax originalDecl)
            {
                // If we introduced this new local declaration statement while computing the code fix, but all it's
                // existing references were removed as part of FixAll, then we can remove the unnecessary local
                // declaration statement. Additionally, if this is an existing local declaration without an initializer,
                // such that the local has no references anymore, we can remove it.

                if (newDecl.HasAnnotation(s_unusedLocalDeclarationAnnotation) ||
                    newDecl.HasAnnotation(s_existingLocalDeclarationWithoutInitializerAnnotation))
                {
                    // Check if we have no references to local in fixed code.
                    if (await IsLocalDeclarationWithNoReferencesAsync(newDecl, document, cancellationToken).ConfigureAwait(false))
                    {
                        var rootWithRemovedDeclaration = root.RemoveNode(newDecl, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepLeadingTrivia);
                        Contract.ThrowIfNull(rootWithRemovedDeclaration);
                        document = document.WithSyntaxRoot(rootWithRemovedDeclaration);
                        return true;
                    }
                }

                return false;
            }
        }

        private static async Task<bool> IsLocalDeclarationWithNoReferencesAsync(
            TLocalDeclarationStatementSyntax declStatement,
            Document document,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var localDeclarationOperation = (IVariableDeclarationGroupOperation)semanticModel.GetRequiredOperation(declStatement, cancellationToken);
            var local = localDeclarationOperation.GetDeclaredVariables().Single();

            // Check if the declared variable has no references in fixed code.
            var referencedSymbols = await SymbolFinder.FindReferencesAsync(local, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            return referencedSymbols.Count() == 1 &&
                referencedSymbols.Single().Locations.IsEmpty();
        }

        protected sealed class UniqueVariableNameGenerator(
            SyntaxNode memberDeclaration,
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken) : IDisposable
        {
            private readonly SyntaxNode _memberDeclaration = memberDeclaration;
            private readonly SemanticModel _semanticModel = semanticModel;
            private readonly ISemanticFactsService _semanticFacts = semanticFacts;
            private readonly CancellationToken _cancellationToken = cancellationToken;
            private readonly PooledHashSet<string> _usedNames = PooledHashSet<string>.GetInstance();

            public SyntaxToken GenerateUniqueNameAtSpanStart(SyntaxNode node)
            {
                var nameToken = _semanticFacts.GenerateUniqueName(_semanticModel, node, _memberDeclaration, "unused", _usedNames, _cancellationToken);
                _usedNames.Add(nameToken.ValueText);
                return nameToken;
            }

            public void Dispose() => _usedNames.Free();
        }
    }
}
