using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UseAutoProperty")]
    internal class UseAutoPropertyCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseAutoPropertyAnalyzer.UseAutoProperty);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var equivalenceKey = diagnostic.Properties[nameof(AnalysisResult.SymbolEquivalenceKey)];

                context.RegisterCodeFix(
                    new UseAutoPropertyCodeAction(
                        CSharpEditorResources.UseAutoProperty,
                        c => ProcessResult(context, diagnostic, c),
                        equivalenceKey),
                    diagnostic);
            }

            return Task.FromResult(false);
        }

        private async Task<Solution> ProcessResult(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var locations = diagnostic.AdditionalLocations;
            var propertyLocation = locations[0];
            var declaratorLocation = locations[1];

            var declarator = declaratorLocation.FindToken(cancellationToken).Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            var fieldDocument = context.Document.Project.GetDocument(declarator.SyntaxTree);
            var fieldSemanticModel = await fieldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var fieldSymbol = (IFieldSymbol)fieldSemanticModel.GetDeclaredSymbol(declarator);

            var property = propertyLocation.FindToken(cancellationToken).Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            var propertyDocument = context.Document.Project.GetDocument(property.SyntaxTree);
            var propertySemanticModel = await propertyDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertySymbol = (IPropertySymbol)propertySemanticModel.GetDeclaredSymbol(property);

            Debug.Assert(fieldDocument.Project == propertyDocument.Project);
            var project = fieldDocument.Project;
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var solution = context.Document.Project.Solution;
            var fieldLocations = await Renamer.GetRenameLocationsAsync(solution, fieldSymbol, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

            // First, create the updated property we want to replace the old property with
            var updatedProperty = UpdateProperty(project, fieldSymbol, propertySymbol, property, fieldLocations, cancellationToken);

            // Now, rename all usages of the field to point at the property.  Except don't actually 
            // rename the field itself.  We want to be able to find it again post rename.
            var updatedSolution = await Renamer.RenameAsync(fieldLocations, propertySymbol.Name,
                location => !location.SourceSpan.IntersectsWith(declaratorLocation.SourceSpan),
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

            declarator = (VariableDeclaratorSyntax)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            property = (PropertyDeclarationSyntax)await propertySymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            var fieldDeclaration = (FieldDeclarationSyntax)declarator.Parent.Parent;
            var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;

            const SyntaxRemoveOptions options = SyntaxRemoveOptions.KeepUnbalancedDirectives | SyntaxRemoveOptions.AddElasticMarker;
            if (fieldDocument == propertyDocument)
            {
                // Same file.  Have to do this in a slightly complicated fashion.
                var declaratorTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var editor = new SyntaxEditor(declaratorTreeRoot, fieldDocument.Project.Solution.Workspace);
                editor.RemoveNode(nodeToRemove, options);
                editor.ReplaceNode(property, updatedProperty);

                return solution.WithDocumentSyntaxRoot(
                    fieldDocument.Id, editor.GetChangedRoot());
            }
            else
            {
                // In different files.  Just update both files.
                var fieldTreeRoot = await fieldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var propertyTreeRoot = await propertyDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newFieldTreeRoot = fieldTreeRoot.RemoveNode(nodeToRemove, options);
                var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(property, updatedProperty);

                updatedSolution = solution.WithDocumentSyntaxRoot(fieldDocument.Id, newFieldTreeRoot);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

                return updatedSolution;
            }
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
                var otherProperty = symbol as IPropertySymbol;
                if (otherProperty != null)
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

        private PropertyDeclarationSyntax UpdateProperty(
            Project project, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol, PropertyDeclarationSyntax propertyDeclaration,
            RenameLocations fieldRenameLocations, CancellationToken cancellationToken)
        {
            var updatedProperty = propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList));

            // We may need to add a setter if the field is written to outside of the constructor
            // of it's class.
            if (AddSetterIfNecessary(fieldSymbol, propertyDeclaration, fieldRenameLocations, cancellationToken))
            {
                var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                var generator = SyntaxGenerator.GetGenerator(project);

                if (fieldSymbol.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                {
                    accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, fieldSymbol.DeclaredAccessibility);
                }

                updatedProperty = updatedProperty.AddAccessorListAccessors(accessor);
            }

            return updatedProperty;
        }

        private bool AddSetterIfNecessary(
            IFieldSymbol fieldSymbol,
            PropertyDeclarationSyntax propertyDeclaration,
            RenameLocations fieldRenameLocations,
            CancellationToken cancellationToken)
        {
            if (propertyDeclaration.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
            {
                // No need to add an setter if we already have one.
                return false;
            }

            // If the original field was written to outside of a constructor (or the property 
            // we're converting), then we'll need to add a setter to the property we're creating.
            var containingTypeNodes = fieldSymbol.ContainingType.DeclaringSyntaxReferences.Select(s => s.GetSyntax(cancellationToken)).ToImmutableArray();

            return fieldRenameLocations.Locations.Any(loc => NeedsSetter(loc, containingTypeNodes, propertyDeclaration, cancellationToken));
        }

        private bool NeedsSetter(
            RenameLocation location,
            ImmutableArray<SyntaxNode> containingTypeNodes,
            PropertyDeclarationSyntax propertyDeclaration,
            CancellationToken cancellationToken)
        {
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
                    // We don't need a setter if we're a reference in the property we're replacing.
                    return false;
                }

                if (node.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    // If we're written to in a constructor in the field's class, we don't need
                    // a setter.
                    if (containingTypeNodes.Contains(node.Parent))
                    {
                        return false;
                    }
                }

                node = node.Parent;
            }

            // We do need a setter
            return true;
        }

        private AccessorListSyntax UpdateAccessorList(AccessorListSyntax accessorList)
        {
            return accessorList.WithAccessors(SyntaxFactory.List(GetAccessors(accessorList.Accessors)));
        }

        private IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
        {
            foreach (var accessor in accessors)
            {
                yield return accessor.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }

        private class UseAutoPropertyCodeAction : CodeAction.SolutionChangeAction
        {
            public UseAutoPropertyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}