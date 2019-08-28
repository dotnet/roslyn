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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    internal abstract class AbstractUseAutoPropertyCodeFixProvider<TTypeDeclarationSyntax, TPropertyDeclaration, TVariableDeclarator, TConstructorDeclaration, TExpression> : CodeFixProvider
        where TTypeDeclarationSyntax : SyntaxNode
        where TPropertyDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TConstructorDeclaration : SyntaxNode
        where TExpression : SyntaxNode
    {
        protected static SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseAutoPropertyDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected abstract SyntaxNode GetNodeToRemove(TVariableDeclarator declarator);

        protected abstract IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document);

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

            return Task.CompletedTask;
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
            var updatedProperty = await UpdatePropertyAsync(
                propertyDocument, compilation, fieldSymbol, propertySymbol, property,
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
            // and the exact doc we're updating the property in.  It can't touch the other linked
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

            // If we have a situation where the property is the second member in a type, and it
            // would become the first, then remove any leading blank lines from it so we don't have
            // random blanks above it that used to space it from the field that was there.
            //
            // The reason we do this special processing is that the first member of a type tends to
            // be special wrt leading trivia. i.e. users do not normally put blank lines before the
            // first member. And so, when a type now becomes the first member, we want to follow the
            // user's common pattern here.
            //
            // In all other code cases, i.e.when there are multiple fields above, or the field is
            // below the property, then the property isn't now becoming "the first member", and as
            // such, it doesn't want this special behavior about it's leading blank lines. i.e. if
            // the user has:
            //
            //  class C
            //  {
            //      int i;
            //      int j;
            //
            //      int Prop => j;
            //  }
            //
            // Then if we remove 'j' (or even 'i'), then 'Prop' would stay the non-first member, and
            // would definitely want to keep that blank line above it.
            //
            // In essence, the blank line above the property exists for separation from what's above
            // it. As long as something is above it, we keep the separation. However, if the
            // property becomes the first member in the type, the separation is now inappropriate
            // because there's nothing to actually separate it from.
            if (fieldDocument == propertyDocument)
            {
                var syntaxFacts = fieldDocument.GetLanguageService<ISyntaxFactsService>();
                if (WillRemoveFirstFieldInTypeDirectlyAboveProperty(syntaxFacts, property, nodeToRemove) &&
                    syntaxFacts.GetLeadingBlankLines(nodeToRemove).Length == 0)
                {
                    updatedProperty = syntaxFacts.GetNodeWithoutLeadingBlankLines(updatedProperty);
                }
            }

            var syntaxRemoveOptions = CreateSyntaxRemoveOptions(nodeToRemove);
            if (fieldDocument == propertyDocument)
            {
                // Same file.  Have to do this in a slightly complicated fashion.
                var declaratorTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var editor = new SyntaxEditor(declaratorTreeRoot, fieldDocument.Project.Solution.Workspace);
                editor.ReplaceNode(property, updatedProperty);
                editor.RemoveNode(nodeToRemove, syntaxRemoveOptions);

                var newRoot = editor.GetChangedRoot();
                newRoot = Format(newRoot, fieldDocument, cancellationToken);

                return solution.WithDocumentSyntaxRoot(fieldDocument.Id, newRoot);
            }
            else
            {
                // In different files.  Just update both files.
                var fieldTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var propertyTreeRoot = await propertyDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newFieldTreeRoot = fieldTreeRoot.RemoveNode(nodeToRemove, syntaxRemoveOptions);
                var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(property, updatedProperty);

                newFieldTreeRoot = Format(newFieldTreeRoot, fieldDocument, cancellationToken);
                newPropertyTreeRoot = Format(newPropertyTreeRoot, propertyDocument, cancellationToken);

                updatedSolution = solution.WithDocumentSyntaxRoot(fieldDocument.Id, newFieldTreeRoot);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

                return updatedSolution;
            }
        }

        private SyntaxRemoveOptions CreateSyntaxRemoveOptions(SyntaxNode nodeToRemove)
        {
            var syntaxRemoveOptions = SyntaxGenerator.DefaultRemoveOptions;
            var hasDirective = nodeToRemove.GetLeadingTrivia().Any(t => t.IsDirective);

            if (hasDirective)
            {
                syntaxRemoveOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            return syntaxRemoveOptions;
        }

        private bool WillRemoveFirstFieldInTypeDirectlyAboveProperty(
            ISyntaxFactsService syntaxFacts, TPropertyDeclaration property, SyntaxNode fieldToRemove)
        {
            if (fieldToRemove.Parent == property.Parent &&
                fieldToRemove.Parent is TTypeDeclarationSyntax typeDeclaration)
            {
                var members = syntaxFacts.GetMembersOfTypeDeclaration(typeDeclaration);
                return members[0] == fieldToRemove && members[1] == property;
            }

            return false;
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

        private SyntaxNode Format(SyntaxNode newRoot, Document document, CancellationToken cancellationToken)
        {
            var formattingRules = GetFormattingRules(document);
            if (formattingRules == null)
            {
                return newRoot;
            }

            return Formatter.Format(newRoot, SpecializedFormattingAnnotation, document.Project.Solution.Workspace, options: null, rules: formattingRules, cancellationToken: cancellationToken);
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
                loc => IsWrittenToOutsideOfConstructorOrProperty(
                    renameLocations.Solution, loc, propertyDeclaration, constructorNodes, cancellationToken));
        }

        private static bool IsWrittenToOutsideOfConstructorOrProperty(
            Solution solution,
            RenameLocation location,
            TPropertyDeclaration propertyDeclaration,
            ISet<TConstructorDeclaration> constructorNodes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!location.IsWrittenTo)
            {
                // We don't need a setter if we're not writing to this field.
                return false;
            }

            var syntaxFacts = solution.GetDocument(location.DocumentId).GetLanguageService<ISyntaxFactsService>();
            var node = location.Location.FindToken(cancellationToken).Parent;

            while (node != null && !syntaxFacts.IsAnonymousOrLocalFunction(node))
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
                Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }
        }
    }
}
