﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    internal abstract class AbstractUseAutoPropertyCodeFixProvider<TPropertyDeclaration, TFieldDeclaration, TVariableDeclarator, TConstructorDeclaration, TExpression> : CodeFixProvider
        where TPropertyDeclaration : SyntaxNode
        where TFieldDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TConstructorDeclaration : SyntaxNode
        where TExpression : SyntaxNode
    {
        protected static SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public sealed override ImmutableArray<string> FixableDiagnosticIds 
            => ImmutableArray.Create(IDEDiagnosticIds.UseAutoPropertyDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected abstract SyntaxNode GetNodeToRemove(TVariableDeclarator declarator);

        protected abstract IEnumerable<IFormattingRule> GetFormattingRules(Document document);

        protected abstract Task<SyntaxNode> UpdatePropertyAsync(
            Document propertyDocument, Compilation compilation, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol,
            TPropertyDeclaration propertyDeclaration, bool isWrittenOutsideConstructor, CancellationToken cancellationToken);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                    ? CodeActionPriority.Low
                    : CodeActionPriority.Medium;

                context.RegisterCodeFix(
                    new UseAutoPropertyCodeAction(
                        FeaturesResources.Use_auto_property,
                        c => ProcessResultAsync(context, diagnostic, c),
                        priority),
                    diagnostic);
            }

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Solution> ProcessResultAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var locations = diagnostic.AdditionalLocations;
            var propertyLocation = locations[0];
            var declaratorLocation = locations[1];

            var declarator = declaratorLocation.FindToken(cancellationToken).Parent.FirstAncestorOrSelf<TVariableDeclarator>();
            var fieldDocument = context.Document.Project.GetDocument(declarator.SyntaxTree);
            var fieldSemanticModel = await fieldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var fieldSymbol = (IFieldSymbol)fieldSemanticModel.GetDeclaredSymbol(declarator);

            var property = propertyLocation.FindToken(cancellationToken).Parent.FirstAncestorOrSelf<TPropertyDeclaration>();
            var propertyDocument = context.Document.Project.GetDocument(property.SyntaxTree);
            var propertySemanticModel = await propertyDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = (IPropertySymbol)propertySemanticModel.GetDeclaredSymbol(property);

            Debug.Assert(fieldDocument.Project == propertyDocument.Project);
            var project = fieldDocument.Project;
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var solution = context.Document.Project.Solution;
            var fieldLocations = await Renamer.GetRenameLocationsAsync(
                solution, SymbolAndProjectId.Create(fieldSymbol, fieldDocument.Project.Id), 
                solution.Options, cancellationToken).ConfigureAwait(false);

            // First, create the updated property we want to replace the old property with
            var isWrittenToOutsideOfConstructor = IsWrittenToOutsideOfConstructorOrProperty(fieldSymbol, fieldLocations, property, cancellationToken);
            var updatedProperty = await UpdatePropertyAsync(propertyDocument, compilation, fieldSymbol, propertySymbol, property,
                isWrittenToOutsideOfConstructor, cancellationToken).ConfigureAwait(false);

            // Note: rename will try to update all the references in linked files as well.  However, 
            // this can lead to some very bad behavior as we will change the references in linked files
            // but only remove the field and update the property in a single document.  So, you can
            // end in the state where you do this in one of the linked file:
            //
            //      int Prop { get { return this.field; } } => int Prop { get { return this.Prop } }
            //
            // But in the main file we'll replace:
            //
            //      int Prop { get { return this.field; } } => int Prop { get; }
            //
            // The workspace will see these as two irreconcilable edits.  To avoid this, we disallow
            // any edits to the other links for the files containing the field and property.  i.e.
            // rename will only be allowed to edit the exact same doc we're removing the field from
            // and the exact doc we're updating hte property in.  It can't touch the other linked
            // files for those docs.  (It can of course touch any other documents unrelated to the
            // docs that the field and prop are declared in).
            var linkedFiles = new HashSet<DocumentId>();
            linkedFiles.AddRange(fieldDocument.GetLinkedDocumentIds());
            linkedFiles.AddRange(propertyDocument.GetLinkedDocumentIds());

            var canEdit = new Dictionary<SyntaxTree, bool>();

            // Now, rename all usages of the field to point at the property.  Except don't actually 
            // rename the field itself.  We want to be able to find it again post rename.
            var updatedSolution = await Renamer.RenameAsync(fieldLocations, propertySymbol.Name,
                location => !location.SourceSpan.IntersectsWith(declaratorLocation.SourceSpan) &&
                            CanEditDocument(solution, location.SourceTree, linkedFiles, canEdit),
                symbols => HasConflict(symbols, propertySymbol, compilation, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            solution = updatedSolution;

            // Now find the field and property again post rename.
            fieldDocument = solution.GetDocument(fieldDocument.Id);
            propertyDocument = solution.GetDocument(propertyDocument.Id);
            Debug.Assert(fieldDocument.Project == propertyDocument.Project);

            compilation = await fieldDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            fieldSymbol = (IFieldSymbol)fieldSymbol.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol;
            propertySymbol = (IPropertySymbol)propertySymbol.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol;
            Debug.Assert(fieldSymbol != null && propertySymbol != null);

            declarator = (TVariableDeclarator)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var temp = await propertySymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            property = temp.FirstAncestorOrSelf<TPropertyDeclaration>();

            var nodeToRemove = GetNodeToRemove(declarator);

            const SyntaxRemoveOptions options = SyntaxRemoveOptions.KeepUnbalancedDirectives | SyntaxRemoveOptions.AddElasticMarker;

            if (fieldDocument == propertyDocument)
            {
                // Same file.  Have to do this in a slightly complicated fashion.
                var declaratorTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var editor = new SyntaxEditor(declaratorTreeRoot, fieldDocument.Project.Solution.Workspace);
                editor.RemoveNode(nodeToRemove, options);
                editor.ReplaceNode(property, updatedProperty);

                var newRoot = editor.GetChangedRoot();
                newRoot = await FormatAsync(newRoot, fieldDocument, cancellationToken).ConfigureAwait(false);

                return solution.WithDocumentSyntaxRoot(
                    fieldDocument.Id, newRoot);
            }
            else
            {
                // In different files.  Just update both files.
                var fieldTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var propertyTreeRoot = await propertyDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newFieldTreeRoot = fieldTreeRoot.RemoveNode(nodeToRemove, options);
                var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(property, updatedProperty);

                newFieldTreeRoot = await FormatAsync(newFieldTreeRoot, fieldDocument, cancellationToken).ConfigureAwait(false);
                newPropertyTreeRoot = await FormatAsync(newPropertyTreeRoot, propertyDocument, cancellationToken).ConfigureAwait(false);

                updatedSolution = solution.WithDocumentSyntaxRoot(fieldDocument.Id, newFieldTreeRoot);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

                return updatedSolution;
            }
        }

        private bool CanEditDocument(
            Solution solution, SyntaxTree sourceTree,
            HashSet<DocumentId> linkedDocuments,
            Dictionary<SyntaxTree, bool> canEdit)
        {
            if (!canEdit.ContainsKey(sourceTree))
            {
                var document = solution.GetDocument(sourceTree);
                canEdit[sourceTree] = document != null && !linkedDocuments.Contains(document.Id);
            }

            return canEdit[sourceTree];
        }

        private async Task<SyntaxNode> FormatAsync(SyntaxNode newRoot, Document document, CancellationToken cancellationToken)
        {
            var formattingRules = GetFormattingRules(document);
            if (formattingRules == null)
            {
                return newRoot;
            }

            return await Formatter.FormatAsync(newRoot, SpecializedFormattingAnnotation, document.Project.Solution.Workspace, options: null, rules: formattingRules, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static bool IsWrittenToOutsideOfConstructorOrProperty(
            IFieldSymbol field, RenameLocations renameLocations, TPropertyDeclaration propertyDeclaration, CancellationToken cancellationToken)
        {
            var constructorNodes = field.ContainingType.GetMembers()
                                                       .Where(m => m.IsConstructor())
                                                       .SelectMany(c => c.DeclaringSyntaxReferences)
                                                       .Select(s => s.GetSyntax(cancellationToken))
                                                       .Select(n => n.FirstAncestorOrSelf<TConstructorDeclaration>())
                                                       .WhereNotNull()
                                                       .ToSet();
            return renameLocations.Locations.Any(
                loc => IsWrittenToOutsideOfConstructorOrProperty(loc, propertyDeclaration, constructorNodes, cancellationToken));
        }

        private static bool IsWrittenToOutsideOfConstructorOrProperty(
            RenameLocation location, TPropertyDeclaration propertyDeclaration, ISet<TConstructorDeclaration> constructorNodes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!location.IsWrittenTo)
            {
                // We don't need a setter if we're not writing to this field.
                return false;
            }

            var node = location.Location.FindToken(cancellationToken).Parent;
            while (node != null)
            {
                if (node == propertyDeclaration)
                {
                    // Not a write outside the property declaration.
                    return false;
                }

                if (constructorNodes.Contains(node))
                {
                    // Not a write outside a constructor of the field's class
                    return false;
                }

                node = node.Parent;
            }

            // We do need a setter
            return true;
        }

        private bool? HasConflict(IEnumerable<ISymbol> symbols, IPropertySymbol property, Compilation compilation, CancellationToken cancellationToken)
        {
            // We're asking the rename API to update a bunch of references to an existing field to
            // the same name as an existing property.  Rename will often flag this situation as
            // an unresolvable conflict because the new name won't bind to the field anymore.
            //
            // To address this, we let rename know that there is no conflict if the new symbol it
            // resolves to is the same as the property we're trying to get the references pointing
            // to.

            foreach (var symbol in symbols)
            {
                if (symbol is IPropertySymbol otherProperty)
                {
                    var mappedProperty = otherProperty.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as IPropertySymbol;
                    if (property.Equals(mappedProperty))
                    {
                        // No conflict.
                        return false;
                    }
                }
            }

            // Just do the default check.
            return null;
        }

        private class UseAutoPropertyCodeAction : CodeAction.SolutionChangeAction
        {
            public UseAutoPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, CodeActionPriority priority)
                : base(title, createChangedSolution, title)
            {
                this.Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }
        }
    }
}
