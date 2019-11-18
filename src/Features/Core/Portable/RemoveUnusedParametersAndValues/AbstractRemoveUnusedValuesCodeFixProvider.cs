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
        private static readonly SyntaxAnnotation s_memberAnnotation = new SyntaxAnnotation();
        private static readonly SyntaxAnnotation s_newLocalDeclarationStatementAnnotation = new SyntaxAnnotation();
        private static readonly SyntaxAnnotation s_unusedLocalDeclarationAnnotation = new SyntaxAnnotation();
        private static readonly SyntaxAnnotation s_existingLocalDeclarationWithoutInitializerAnnotation = new SyntaxAnnotation();

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId,
                                                                                                    IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

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

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            if (!AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.TryGetUnusedValuePreference(diagnostic, out var preference))
            {
                return Task.CompletedTask;
            }

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
                        if (IsForEachIterationVariableDiagnostic(diagnostic, context.Document, context.CancellationToken))
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

        private static bool IsForEachIterationVariableDiagnostic(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            // Do not offer a fix to replace unused foreach iteration variable with discard.
            // User should probably replace it with a for loop based on the collection length.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return syntaxFacts.IsForEachStatement(diagnostic.Location.FindNode(cancellationToken));
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

        protected override bool IncludeDiagnosticDuringFixAll(FixAllState fixAllState, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return fixAllState.CodeActionEquivalenceKey == GetEquivalenceKey(diagnostic) &&
                !IsForEachIterationVariableDiagnostic(diagnostic, fixAllState.Document, cancellationToken);
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

        private IEnumerable<IGrouping<SyntaxNode, Diagnostic>> GetDiagnosticsGroupedByMember(
            ImmutableArray<Diagnostic> diagnostics,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode root)
            => diagnostics.GroupBy(d => syntaxFacts.GetContainingMemberDeclaration(root, d.Location.SourceSpan.Start));

        private async Task<Document> PreprocessDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            // Track all the member declaration nodes that have diagnostics.
            // We will post process all these tracked nodes after applying the fix (see "PostProcessDocumentAsync" below in this source file).

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var memberDeclarations = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root).Select(g => g.Key);
            root = root.ReplaceNodes(memberDeclarations, computeReplacementNode: (_, n) => n.WithAdditionalAnnotations(s_memberAnnotation));
            return document.WithSyntaxRoot(root);
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            document = await PreprocessDocumentAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var originalEditor = editor;
            editor = new SyntaxEditor(root, editor.Generator);

            try
            {
                // We compute the code fix in two passes:
                //   1. The first pass groups the diagnostics to fix by containing member declaration and
                //      computes and applies the core code fixes. Grouping is done to ensure we choose
                //      the most appropriate name for new unused local declarations, which can clash
                //      with existing local declarations in the method body.
                //   2. Second pass (PostProcessDocumentAsync) performs additional syntax manipulations
                //      for the fixes produced from from first pass:
                //      a. Replace discard declarations, such as "var _ = M();" that conflict with newly added
                //         discard assignments, with discard assignments of the form "_ = M();"
                //      b. Move newly introduced local declaration statements closer to the local variable's
                //         first reference.

                // Get diagnostics grouped by member.
                var diagnosticsGroupedByMember = GetDiagnosticsGroupedByMember(diagnostics, syntaxFacts, root,
                    out var diagnosticId, out var preference, out var removeAssignments);

                // First pass to compute and apply the core code fixes.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                foreach (var diagnosticsToFix in diagnosticsGroupedByMember)
                {
                    var orderedDiagnostics = diagnosticsToFix.OrderBy(d => d.Location.SourceSpan.Start);
                    var containingMemberDeclaration = diagnosticsToFix.Key;
                    using var nameGenerator = new UniqueVariableNameGenerator(containingMemberDeclaration, semanticModel, semanticFacts, cancellationToken);

                    await FixAllAsync(diagnosticId, orderedDiagnostics, document, semanticModel, root, containingMemberDeclaration, preference,
                        removeAssignments, nameGenerator, editor, syntaxFacts, cancellationToken).ConfigureAwait(false);
                }

                // Second pass to post process the document.
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
                originalEditor.ReplaceNode(originalEditor.OriginalRoot, editor.GetChangedRoot());
            }
        }

        private async Task FixAllAsync(
            string diagnosticId,
            IOrderedEnumerable<Diagnostic> diagnostics,
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            SyntaxNode containingMemberDeclaration,
            UnusedValuePreference preference,
            bool removeAssignments,
            UniqueVariableNameGenerator nameGenerator,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId:
                    FixAllExpressionValueIsUnusedDiagnostics(diagnostics, semanticModel, root,
                        preference, nameGenerator, editor, syntaxFacts);
                    break;

                case IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId:
                    await FixAllValueAssignedIsUnusedDiagnosticsAsync(diagnostics, document, semanticModel, root, containingMemberDeclaration,
                        preference, removeAssignments, nameGenerator, editor, syntaxFacts, cancellationToken).ConfigureAwait(false);
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
            UniqueVariableNameGenerator nameGenerator,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts)
        {
            // This method applies the code fix for diagnostics reported for expression statement dropping values.
            // We replace each flagged expression statement with an assignment to a discard variable or a new unused local,
            // based on the user's preference.

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
                                                                left: editor.Generator.IdentifierName(AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName),
                                                                right: expression.WithoutTrivia())
                                                            .WithTriviaFrom(expression)
                                                            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                        editor.ReplaceNode(expression, discardAssignmentExpression);
                        break;

                    case UnusedValuePreference.UnusedLocalVariable:
                        // Add Simplifier annotation so that 'var'/explicit type is correctly added based on user options.
                        var localDecl = editor.Generator.LocalDeclarationStatement(
                                            name: nameGenerator.GenerateUniqueNameAtSpanStart(expressionStatement),
                                            initializer: expression.WithoutLeadingTrivia())
                                        .WithTriviaFrom(expressionStatement)
                                        .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
                        editor.ReplaceNode(expressionStatement, localDecl);
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
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken)
        {
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

            var nodeReplacementMap = PooledDictionary<SyntaxNode, SyntaxNode>.GetInstance();
            var nodesToRemove = PooledHashSet<SyntaxNode>.GetInstance();
            var nodesToAdd = PooledHashSet<(TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode node)>.GetInstance();
            // Indicates if the node's trivia was processed.
            var processedNodes = PooledHashSet<SyntaxNode>.GetInstance();
            var candidateDeclarationStatementsForRemoval = PooledHashSet<TLocalDeclarationStatementSyntax>.GetInstance();
            var hasAnyUnusedLocalAssignment = false;

            try
            {
                foreach (var (node, isUnusedLocalAssignment) in GetNodesToFix())
                {
                    hasAnyUnusedLocalAssignment |= isUnusedLocalAssignment;

                    var declaredLocal = semanticModel.GetDeclaredSymbol(node, cancellationToken) as ILocalSymbol;
                    if (declaredLocal == null && node.Parent is TCatchStatementSyntax)
                    {
                        declaredLocal = semanticModel.GetDeclaredSymbol(node.Parent, cancellationToken) as ILocalSymbol;
                    }

                    string newLocalNameOpt = null;
                    if (removeAssignments)
                    {
                        // Removable assignment or initialization, such that right hand side has no side effects.
                        if (declaredLocal != null)
                        {
                            // Redundant initialization.
                            // For example, "int a = 0;"
                            var variableDeclarator = node.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
                            Debug.Assert(variableDeclarator != null);
                            nodesToRemove.Add(variableDeclarator);

                            // Local declaration statement containing the declarator might be a candidate for removal if all its variables get marked for removal.
                            candidateDeclarationStatementsForRemoval.Add(variableDeclarator.GetAncestor<TLocalDeclarationStatementSyntax>());
                        }
                        else
                        {
                            // Redundant assignment or increment/decrement.
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
                                else if (node is
                                {
                                    Parent: TExpressionSyntax { Parent: TExpressionStatementSyntax _ }
                                })
                                {
                                    // For example, C# simple assignment statement "a = 0;"
                                    nodesToRemove.Add(node.Parent.Parent);
                                }
                                else
                                {
                                    // For example, C# nested assignment statement "a = b = 0;", where assignment to 'b' is redundant.
                                    // We replace the node with "a = 0;"
                                    nodeReplacementMap.Add(node.Parent, syntaxFacts.GetRightHandSideOfAssignment(node.Parent));
                                }
                            }
                        }
                    }
                    else
                    {
                        // Value initialization/assignment where the right hand side may have side effects,
                        // and hence needs to be preserved in fixed code.
                        // For example, "x = MethodCall();" is replaced with "_ = MethodCall();" or "var unused = MethodCall();"

                        // Replace the flagged variable's indentifier token with new named, based on user's preference.
                        var newNameToken = preference == UnusedValuePreference.DiscardVariable
                            ? editor.Generator.Identifier(AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName)
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
                            nodeReplacementMap.Add(node.Parent, GetReplacementNodeForCompoundAssignment(node.Parent, newNameNode, editor, syntaxFacts));
                        }
                        else
                        {
                            nodeReplacementMap.Add(node, newNameNode);
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
                            Debug.Assert(type != null);
                            Debug.Assert(newLocalNameOpt != null);
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
                    // We annotate such declaration statements with no initializer abd non-zero references here
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

                foreach (var kvp in nodeReplacementMap)
                {
                    editor.ReplaceNode(kvp.Key, kvp.Value.WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            finally
            {
                nodeReplacementMap.Free();
                nodesToRemove.Free();
                nodesToAdd.Free();
                processedNodes.Free();
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
                // Find the correct place to insert the given declaration statement based on the node's ancestors.
                var insertionNode = node.FirstAncestorOrSelf<SyntaxNode>(n => n.Parent is TSwitchCaseBlockSyntax ||
                                                                              syntaxFacts.IsExecutableBlock(n.Parent) &&
                                                                              !(n is TCatchStatementSyntax) &&
                                                                              !(n is TCatchBlockSyntax));
                if (insertionNode is TSwitchCaseLabelOrClauseSyntax)
                {
                    InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(insertionNode.GetAncestor<TSwitchCaseBlockSyntax>(), editor, declarationStatement);
                }
                else if (insertionNode is TStatementSyntax)
                {
                    // If the insertion node is being removed, keep the leading trivia with the new declaration.
                    if (nodesToRemove.Contains(insertionNode) && !processedNodes.Contains(insertionNode))
                    {
                        declarationStatement = declarationStatement.WithLeadingTrivia(insertionNode.GetLeadingTrivia());
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
                    ReplaceDiscardDeclarationsWithAssignmentsAsync, currentRoot, document, cancellationToken).ConfigureAwait(false);
            }

            // If we added new variable declaration statements, move these as close as possible to their
            // first reference site.
            if (NeedsToMoveNewLocalDeclarationsNearReference(diagnosticId))
            {
                currentRoot = await PostProcessDocumentCoreAsync(
                    AdjustLocalDeclarationsAsync, currentRoot, document, cancellationToken).ConfigureAwait(false);
            }

            return currentRoot;
        }

        private static async Task<SyntaxNode> PostProcessDocumentCoreAsync(
            Func<SyntaxNode, Document, CancellationToken, Task<SyntaxNode>> processMemberDeclarationAsync,
            SyntaxNode currentRoot,
            Document document,
            CancellationToken cancellationToken)
        {
            // Process each member declaration which had atleast one diagnostic reported in the original tree
            // and hence was annotated with "s_memberAnnotation" for post processing.

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
        private async Task<SyntaxNode> ReplaceDiscardDeclarationsWithAssignmentsAsync(SyntaxNode memberDeclaration, Document document, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IReplaceDiscardDeclarationsWithAssignmentsService>();
            if (service == null)
            {
                return memberDeclaration;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await service.ReplaceAsync(memberDeclaration, semanticModel, cancellationToken).ConfigureAwait(false);
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
            CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IMoveDeclarationNearReferenceService>();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var originalDocument = document;
            var originalDeclStatementsToMoveOrRemove = memberDeclaration.DescendantNodes()
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
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rootWithTrackedNodes = root.TrackNodes(originalDeclStatementsToMoveOrRemove);

            // Run formatter prior to invoking IMoveDeclarationNearReferenceService.
            rootWithTrackedNodes = Formatter.Format(rootWithTrackedNodes, originalDeclStatementsToMoveOrRemove.Select(s => s.Span), document.Project.Solution.Workspace, cancellationToken: cancellationToken);

            document = document.WithSyntaxRoot(rootWithTrackedNodes);
            await OnDocumentUpdatedAsync().ConfigureAwait(false);

            foreach (TLocalDeclarationStatementSyntax originalDeclStatement in originalDeclStatementsToMoveOrRemove)
            {
                // Get the current declaration statement.
                var declStatement = memberDeclaration.GetCurrentNode(originalDeclStatement);

                var documentUpdated = false;

                // Check if the variable declaration is unused after all the fixes, and hence can be removed.
                if (await TryRemoveUnusedLocalAsync(declStatement, originalDeclStatement).ConfigureAwait(false))
                {
                    documentUpdated = true;
                }
                else if (declStatement.HasAnnotation(s_newLocalDeclarationStatementAnnotation))
                {
                    // Otherwise, move the declaration closer to the first reference if possible.
                    if (await service.CanMoveDeclarationNearReferenceAsync(document, declStatement, cancellationToken).ConfigureAwait(false))
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

            async Task<bool> TryRemoveUnusedLocalAsync(TLocalDeclarationStatementSyntax newDecl, TLocalDeclarationStatementSyntax originalDecl)
            {
                // If we introduced this new local declaration statement while computing the code fix,
                // but all it's existing references were removed as part of FixAll, then we
                // can remove the unncessary local declaration statement.
                // Additionally, if this is an existing local declaration without an initializer,
                // such that the local has no references anymore, we can remove it.

                if (newDecl.HasAnnotation(s_unusedLocalDeclarationAnnotation) ||
                    newDecl.HasAnnotation(s_existingLocalDeclarationWithoutInitializerAnnotation))
                {
                    // Check if we have no references to local in fixed code.
                    if (await IsLocalDeclarationWithNoReferencesAsync(newDecl, document, cancellationToken).ConfigureAwait(false))
                    {
                        document = document.WithSyntaxRoot(
                        root.RemoveNode(newDecl, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepLeadingTrivia));
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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var localDeclarationOperation = (IVariableDeclarationGroupOperation)semanticModel.GetOperation(declStatement, cancellationToken);
            var local = localDeclarationOperation.GetDeclaredVariables().Single();

            // Check if the declared variable has no references in fixed code.
            var referencedSymbols = await SymbolFinder.FindReferencesAsync(local, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            return referencedSymbols.Count() == 1 &&
                referencedSymbols.Single().Locations.IsEmpty();
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }

        protected sealed class UniqueVariableNameGenerator : IDisposable
        {
            private readonly SyntaxNode _memberDeclaration;
            private readonly SemanticModel _semanticModel;
            private readonly ISemanticFactsService _semanticFacts;
            private readonly CancellationToken _cancellationToken;
            private readonly PooledHashSet<string> _usedNames;

            public UniqueVariableNameGenerator(
                SyntaxNode memberDeclaration,
                SemanticModel semanticModel,
                ISemanticFactsService semanticFacts,
                CancellationToken cancellationToken)
            {
                _memberDeclaration = memberDeclaration;
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _cancellationToken = cancellationToken;

                _usedNames = PooledHashSet<string>.GetInstance();
            }

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
