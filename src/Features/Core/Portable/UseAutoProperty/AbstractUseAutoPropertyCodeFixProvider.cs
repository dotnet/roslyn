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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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
        protected static SyntaxAnnotation SpecializedFormattingAnnotation = new();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseAutoPropertyDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected abstract TPropertyDeclaration GetPropertyDeclaration(SyntaxNode node);
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
                        AnalyzersResources.Use_auto_property,
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

            var solution = context.Document.Project.Solution;
            var declarator = (TVariableDeclarator)declaratorLocation.FindNode(cancellationToken);
            var fieldDocument = solution.GetRequiredDocument(declarator.SyntaxTree);
            var fieldSemanticModel = await fieldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var fieldSymbol = (IFieldSymbol)fieldSemanticModel.GetRequiredDeclaredSymbol(declarator, cancellationToken);

            var property = GetPropertyDeclaration(propertyLocation.FindNode(cancellationToken));
            var propertyDocument = solution.GetRequiredDocument(property.SyntaxTree);
            var propertySemanticModel = await propertyDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = (IPropertySymbol)propertySemanticModel.GetRequiredDeclaredSymbol(property, cancellationToken);

            Debug.Assert(fieldDocument.Project == propertyDocument.Project);
            var project = fieldDocument.Project;
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var renameOptions = new SymbolRenameOptions();

            var fieldLocations = await Renamer.FindRenameLocationsAsync(
                solution, fieldSymbol, renameOptions, context.Options, cancellationToken).ConfigureAwait(false);

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

            var canEdit = new Dictionary<DocumentId, bool>();

            // Now, rename all usages of the field to point at the property.  Except don't actually 
            // rename the field itself.  We want to be able to find it again post rename.
            //
            // We're asking the rename API to update a bunch of references to an existing field to the same name as an
            // existing property.  Rename will often flag this situation as an unresolvable conflict because the new
            // name won't bind to the field anymore.
            //
            // To address this, we let rename know that there is no conflict if the new symbol it resolves to is the
            // same as the property we're trying to get the references pointing to.

            var filteredLocations = fieldLocations.Filter(
                (documentId, span) =>
                    fieldDocument.Id == documentId ? !span.IntersectsWith(declaratorLocation.SourceSpan) : true && // The span check only makes sense if we are in the same file
                    CanEditDocument(solution, documentId, linkedFiles, canEdit));

            var resolution = await filteredLocations.ResolveConflictsAsync(
                fieldSymbol, propertySymbol.Name,
                nonConflictSymbolKeys: ImmutableArray.Create(propertySymbol.GetSymbolKey(cancellationToken)), cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(resolution.IsSuccessful);

            solution = resolution.NewSolution;

            // Now find the field and property again post rename.
            fieldDocument = solution.GetRequiredDocument(fieldDocument.Id);
            propertyDocument = solution.GetRequiredDocument(propertyDocument.Id);
            Debug.Assert(fieldDocument.Project == propertyDocument.Project);

            compilation = await fieldDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            fieldSymbol = (IFieldSymbol?)fieldSymbol.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol;
            propertySymbol = (IPropertySymbol?)propertySymbol.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol;
            Contract.ThrowIfTrue(fieldSymbol == null || propertySymbol == null);

            declarator = (TVariableDeclarator)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            property = GetPropertyDeclaration(await propertySymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false));

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
                var syntaxFacts = fieldDocument.GetRequiredLanguageService<ISyntaxFactsService>();
                var bannerService = fieldDocument.GetRequiredLanguageService<IFileBannerFactsService>();
                if (WillRemoveFirstFieldInTypeDirectlyAboveProperty(syntaxFacts, property, nodeToRemove) &&
                    bannerService.GetLeadingBlankLines(nodeToRemove).Length == 0)
                {
                    updatedProperty = bannerService.GetNodeWithoutLeadingBlankLines(updatedProperty);
                }
            }

            var syntaxRemoveOptions = CreateSyntaxRemoveOptions(nodeToRemove);
            if (fieldDocument == propertyDocument)
            {
                // Same file.  Have to do this in a slightly complicated fashion.
                var declaratorTreeRoot = await fieldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var editor = new SyntaxEditor(declaratorTreeRoot, fieldDocument.Project.Solution.Services);
                editor.ReplaceNode(property, updatedProperty);
                editor.RemoveNode(nodeToRemove, syntaxRemoveOptions);

                var newRoot = editor.GetChangedRoot();
                newRoot = await FormatAsync(newRoot, fieldDocument, context.Options, cancellationToken).ConfigureAwait(false);

                return solution.WithDocumentSyntaxRoot(fieldDocument.Id, newRoot);
            }
            else
            {
                // In different files.  Just update both files.
                var fieldTreeRoot = await fieldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var propertyTreeRoot = await propertyDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newFieldTreeRoot = fieldTreeRoot.RemoveNode(nodeToRemove, syntaxRemoveOptions);
                Contract.ThrowIfNull(newFieldTreeRoot);
                var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(property, updatedProperty);

                newFieldTreeRoot = await FormatAsync(newFieldTreeRoot, fieldDocument, context.Options, cancellationToken).ConfigureAwait(false);
                newPropertyTreeRoot = await FormatAsync(newPropertyTreeRoot, propertyDocument, context.Options, cancellationToken).ConfigureAwait(false);

                var updatedSolution = solution.WithDocumentSyntaxRoot(fieldDocument.Id, newFieldTreeRoot);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

                return updatedSolution;
            }
        }

        private static SyntaxRemoveOptions CreateSyntaxRemoveOptions(SyntaxNode nodeToRemove)
        {
            var syntaxRemoveOptions = SyntaxGenerator.DefaultRemoveOptions;
            var hasDirective = nodeToRemove.GetLeadingTrivia().Any(t => t.IsDirective);

            if (hasDirective)
            {
                syntaxRemoveOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            return syntaxRemoveOptions;
        }

        private static bool WillRemoveFirstFieldInTypeDirectlyAboveProperty(
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

        private static bool CanEditDocument(
            Solution solution,
            DocumentId documentId,
            HashSet<DocumentId> linkedDocuments,
            Dictionary<DocumentId, bool> canEdit)
        {
            if (!canEdit.TryGetValue(documentId, out var canEditDocument))
            {
                var document = solution.GetDocument(documentId);
                canEditDocument = document != null && !linkedDocuments.Contains(document.Id);
                canEdit[documentId] = canEditDocument;
            }

            return canEditDocument;
        }

        private async Task<SyntaxNode> FormatAsync(SyntaxNode newRoot, Document document, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var formattingRules = GetFormattingRules(document);
            if (formattingRules == null)
            {
                return newRoot;
            }

            var options = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            return Formatter.Format(newRoot, SpecializedFormattingAnnotation, document.Project.Solution.Services, options, formattingRules, cancellationToken);
        }

        private static bool IsWrittenToOutsideOfConstructorOrProperty(
            IFieldSymbol field, LightweightRenameLocations renameLocations, TPropertyDeclaration propertyDeclaration, CancellationToken cancellationToken)
        {
            var constructorSpans = field.ContainingType.GetMembers()
                                                       .Where(m => m.IsConstructor())
                                                       .SelectMany(c => c.DeclaringSyntaxReferences)
                                                       .Select(s => s.GetSyntax(cancellationToken))
                                                       .Select(n => n.FirstAncestorOrSelf<TConstructorDeclaration>())
                                                       .WhereNotNull()
                                                       .Select(d => (d.SyntaxTree.FilePath, d.Span))
                                                       .ToSet();
            return renameLocations.Locations.Any(
                loc => IsWrittenToOutsideOfConstructorOrProperty(
                    renameLocations.Solution, loc, propertyDeclaration, constructorSpans, cancellationToken));
        }

        private static bool IsWrittenToOutsideOfConstructorOrProperty(
            Solution solution,
            RenameLocation location,
            TPropertyDeclaration propertyDeclaration,
            ISet<(string filePath, TextSpan span)> constructorSpans,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!location.IsWrittenTo)
            {
                // We don't need a setter if we're not writing to this field.
                return false;
            }

            var syntaxFacts = solution.GetRequiredDocument(location.DocumentId).GetRequiredLanguageService<ISyntaxFactsService>();
            var node = location.Location.FindToken(cancellationToken).Parent;

            while (node != null && !syntaxFacts.IsAnonymousOrLocalFunction(node))
            {
                if (node == propertyDeclaration)
                {
                    // Not a write outside the property declaration.
                    return false;
                }

                if (constructorSpans.Contains((node.SyntaxTree.FilePath, node.Span)))
                {
                    // Not a write outside a constructor of the field's class
                    return false;
                }

                node = node.Parent;
            }

            // We do need a setter
            return true;
        }

        private class UseAutoPropertyCodeAction : CustomCodeActions.SolutionChangeAction
        {
            public UseAutoPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution
#if !CODE_STYLE // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
                , CodeActionPriority priority
#endif
                )
                : base(title, createChangedSolution, title)
            {
#if !CODE_STYLE // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
                Priority = priority;
#endif
            }

#if !CODE_STYLE // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
            internal override CodeActionPriority Priority { get; }
#endif
        }
    }
}
