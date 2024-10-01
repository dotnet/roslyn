// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
   Name = PredefinedCodeRefactoringProviderNames.ReplacePropertyWithMethods), Shared]
internal class ReplacePropertyWithMethodsCodeRefactoringProvider :
    CodeRefactoringProvider,
    IEqualityComparer<(IPropertySymbol property, ReferenceLocation location)>
{
    private const string GetPrefix = "Get";
    private const string SetPrefix = "Set";

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public ReplacePropertyWithMethodsCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;
        var service = document.GetLanguageService<IReplacePropertyWithMethodsService>();
        if (service == null)
            return;

        var propertyDeclaration = await service.GetPropertyDeclarationAsync(context).ConfigureAwait(false);
        if (propertyDeclaration == null)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;

        Contract.ThrowIfNull(propertySymbol);
        var propertyName = propertySymbol.Name;

        var accessorCount =
            (propertySymbol.GetMethod == null ? 0 : 1) +
            (propertySymbol.SetMethod == null ? 0 : 1);

        var resourceString = accessorCount == 1
            ? FeaturesResources.Replace_0_with_method
            : FeaturesResources.Replace_0_with_methods;

        context.RegisterRefactoring(
            CodeAction.Create(
                string.Format(resourceString, propertyName),
                c => ReplacePropertyWithMethodsAsync(document, propertySymbol, c),
                propertyName),
            propertyDeclaration.Span);
    }

    private async Task<Solution> ReplacePropertyWithMethodsAsync(
       Document document,
       IPropertySymbol propertySymbol,
       CancellationToken cancellationToken)
    {
        var desiredMethodSuffix = NameGenerator.GenerateUniqueName(propertySymbol.Name,
            n => !HasAnyMatchingGetOrSetMethods(propertySymbol, n));

        var desiredGetMethodName = GetPrefix + desiredMethodSuffix;
        var desiredSetMethodName = SetPrefix + desiredMethodSuffix;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var originalSolution = document.Project.Solution;
        var propertyReferences = await SymbolFinder.FindReferencesAsync(propertySymbol, originalSolution, cancellationToken).ConfigureAwait(false);

        // Get the warnings we'd like to put at the definition site.
        var definitionWarning = GetDefinitionIssues(propertyReferences);
        var definitionToBackingField = CreateDefinitionToBackingFieldMap(propertyReferences);

        var q = from r in propertyReferences
                where r.Definition is IPropertySymbol
                from loc in r.Locations
                select (property: (IPropertySymbol)r.Definition, location: loc);

        var referencesByDocument = q.ToLookup(t => t.location.Document);

        // References and definitions can overlap (for example, references to one property
        // inside the definition of another).  So we do a multi phase rewrite.  We first
        // rewrite all references to point at the new methods instead.  Then we remove all
        // the actual property definitions and replace them with the new methods.
        var updatedSolution = originalSolution;

        updatedSolution = await UpdateReferencesAsync(
            updatedSolution, referencesByDocument, definitionToBackingField,
            desiredGetMethodName, desiredSetMethodName, cancellationToken).ConfigureAwait(false);

        updatedSolution = await ReplaceDefinitionsWithMethodsAsync(
            originalSolution, updatedSolution, propertyReferences, definitionToBackingField,
            desiredGetMethodName, desiredSetMethodName, cancellationToken).ConfigureAwait(false);

        return updatedSolution;
    }

    private static ImmutableDictionary<IPropertySymbol, IFieldSymbol?> CreateDefinitionToBackingFieldMap(IEnumerable<ReferencedSymbol> propertyReferences)
    {
        var definitionToBackingField = ImmutableDictionary.CreateBuilder<IPropertySymbol, IFieldSymbol?>(SymbolEquivalenceComparer.Instance);

        foreach (var reference in propertyReferences)
        {
            if (reference.Definition is IPropertySymbol property)
            {
                var backingField = GetBackingField(property);
                definitionToBackingField[property] = backingField;
            }
        }

        return definitionToBackingField.ToImmutable();
    }

    private static bool HasAnyMatchingGetOrSetMethods(IPropertySymbol property, string name)
    {
        return HasAnyMatchingGetMethods(property, name) ||
            HasAnyMatchingSetMethods(property, name);
    }

    private static bool HasAnyMatchingGetMethods(IPropertySymbol property, string name)
    {
        return property.GetMethod != null &&
               property.ContainingType.GetMembers(GetPrefix + name)
                                      .OfType<IMethodSymbol>()
                                      .Any(m => m.Parameters.Length == 0);
    }

    private static bool HasAnyMatchingSetMethods(IPropertySymbol property, string name)
    {
        var comparer = SymbolEquivalenceComparer.Instance.SignatureTypeEquivalenceComparer;
        return property.SetMethod != null &&
               property.ContainingType
                      .GetMembers(SetPrefix + name)
                      .OfType<IMethodSymbol>()
                      .Any(m => m.Parameters.Length == 1 &&
                                comparer.Equals(m.Parameters[0].Type, property.Type));
    }

    private static IFieldSymbol? GetBackingField(IPropertySymbol property)
    {
        var field = property.GetBackingFieldIfAny();
        if (field == null)
            return null;

        // If the field is something can be referenced with the name it has, then just use
        // it as the backing field we'll generate.  This is the case in VB where the backing
        // field can be referenced as is.
        if (field.CanBeReferencedByName)
            return field;

        // Otherwise, generate a good name for the backing field we're generating.  This is
        // the case for C# where we have mangled names for the backing field and need something
        // actually usable in code.
        var uniqueName = NameGenerator.GenerateUniqueName(
            property.Name.ToCamelCase(),
            n => !property.ContainingType.GetMembers(n).Any());

        return CodeGenerationSymbolFactory.CreateFieldSymbol(
            attributes: default,
            accessibility: field.DeclaredAccessibility,
            modifiers: DeclarationModifiers.From(field),
            type: field.Type,
            name: uniqueName);
    }

#pragma warning disable IDE0060 // Remove unused parameter - Method not completely implemented.
    private static string? GetDefinitionIssues(IEnumerable<ReferencedSymbol> getMethodReferences)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // TODO: add things to be concerned about here.  For example:
        // 1. If any of the referenced symbols are from metadata.
        // 2. If a symbol is referenced implicitly.
        // 3. if the property has attributes.
        return null;
    }

    private async Task<Solution> UpdateReferencesAsync(
        Solution updatedSolution,
        ILookup<Document, (IPropertySymbol property, ReferenceLocation location)> referencesByDocument,
        ImmutableDictionary<IPropertySymbol, IFieldSymbol?> propertyToBackingField,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        foreach (var group in referencesByDocument)
        {
            cancellationToken.ThrowIfCancellationRequested();

            updatedSolution = await UpdateReferencesInDocumentAsync(
                updatedSolution, group.Key, group, propertyToBackingField,
                desiredGetMethodName, desiredSetMethodName, cancellationToken).ConfigureAwait(false);
        }

        return updatedSolution;
    }

    private async Task<Solution> UpdateReferencesInDocumentAsync(
        Solution updatedSolution,
        Document originalDocument,
        IEnumerable<(IPropertySymbol property, ReferenceLocation location)> references,
        ImmutableDictionary<IPropertySymbol, IFieldSymbol?> propertyToBackingField,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        var root = await originalDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, originalDocument.Project.Solution.Services);
        var service = originalDocument.GetRequiredLanguageService<IReplacePropertyWithMethodsService>();

        await ReplaceReferencesAsync(
            originalDocument, references, propertyToBackingField, root, editor, service,
            desiredGetMethodName, desiredSetMethodName, cancellationToken).ConfigureAwait(false);

        updatedSolution = updatedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

        return updatedSolution;
    }

    private async Task ReplaceReferencesAsync(
        Document originalDocument,
        IEnumerable<(IPropertySymbol property, ReferenceLocation location)> references,
        IDictionary<IPropertySymbol, IFieldSymbol?> propertyToBackingField,
        SyntaxNode root, SyntaxEditor editor,
        IReplacePropertyWithMethodsService service,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        if (references != null)
        {
            var syntaxFacts = originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();

            // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol
            // is allowed to report the entire set of references it think it is compatible with.  So ensure we're 
            // hitting each location only once.
            // 
            // Note Use DistinctBy (.Net6) once available.
            foreach (var (property, referenceLocation) in references.Distinct(this))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var location = referenceLocation.Location;
                var nameToken = root.FindToken(location.SourceSpan.Start, findInsideTrivia: true);

                var parent = nameToken.Parent;
                Contract.ThrowIfNull(parent);

                if (referenceLocation.IsImplicit || !syntaxFacts.IsIdentifierName(parent))
                {
                    // Warn the user that we can't properly replace this property with a method.
                    editor.ReplaceNode(parent, parent.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(FeaturesResources.Property_referenced_implicitly)));
                }
                else if (syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(parent))
                {
                    editor.ReplaceNode(parent, parent.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(FeaturesResources.Property_reference_cannot_be_updated)));
                }
                else if (syntaxFacts.IsNameOfSubpattern(parent))
                {
                    editor.ReplaceNode(parent, parent.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(FeaturesResources.Property_reference_cannot_be_updated)));
                }
                else
                {
                    var fieldSymbol = propertyToBackingField.GetValueOrDefault(property);
                    await service.ReplaceReferenceAsync(
                        originalDocument, editor, parent,
                        property, fieldSymbol,
                        desiredGetMethodName, desiredSetMethodName,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task<Solution> ReplaceDefinitionsWithMethodsAsync(
        Solution originalSolution,
        Solution updatedSolution,
        IEnumerable<ReferencedSymbol> references,
        ImmutableDictionary<IPropertySymbol, IFieldSymbol?> definitionToBackingField,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        var definitionsByDocumentId = await GetDefinitionsByDocumentIdAsync(originalSolution, references, cancellationToken).ConfigureAwait(false);

        foreach (var (documentId, definitions) in definitionsByDocumentId)
        {
            cancellationToken.ThrowIfCancellationRequested();

            updatedSolution = await ReplaceDefinitionsWithMethodsAsync(
                updatedSolution, documentId, definitions, definitionToBackingField,
                desiredGetMethodName, desiredSetMethodName, cancellationToken).ConfigureAwait(false);
        }

        return updatedSolution;
    }

    private static async Task<MultiDictionary<DocumentId, IPropertySymbol>> GetDefinitionsByDocumentIdAsync(
       Solution originalSolution,
       IEnumerable<ReferencedSymbol> referencedSymbols,
       CancellationToken cancellationToken)
    {
        var result = new MultiDictionary<DocumentId, IPropertySymbol>();
        foreach (var referencedSymbol in referencedSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definition = referencedSymbol.Definition as IPropertySymbol;
            if (definition?.DeclaringSyntaxReferences.Length > 0)
            {
                var syntax = await definition.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                if (syntax != null)
                {
                    var document = originalSolution.GetDocument(syntax.SyntaxTree);
                    if (document != null)
                    {
                        result.Add(document.Id, definition);
                    }
                }
            }
        }

        return result;
    }

    private static async Task<Solution> ReplaceDefinitionsWithMethodsAsync(
        Solution updatedSolution,
        DocumentId documentId,
        MultiDictionary<DocumentId, IPropertySymbol>.ValueSet originalDefinitions,
        IDictionary<IPropertySymbol, IFieldSymbol?> definitionToBackingField,
        string desiredGetMethodName, string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        var updatedDocument = updatedSolution.GetRequiredDocument(documentId);
        var semanticModel = await updatedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // We've already gone and updated all references.  So now re-resolve all the definitions
        // in the current compilation to find their updated location.
        var currentDefinitions = await GetCurrentPropertiesAsync(
            updatedSolution, semanticModel.Compilation, documentId, originalDefinitions, cancellationToken).ConfigureAwait(false);

        var service = updatedDocument.GetRequiredLanguageService<IReplacePropertyWithMethodsService>();

        var root = await updatedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, updatedSolution.Services);

        // First replace all the properties with the appropriate getters/setters.
        foreach (var (property, declaration) in currentDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var members = await service.GetReplacementMembersAsync(
                updatedDocument,
                property, declaration,
                definitionToBackingField.GetValueOrDefault(property),
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken).ConfigureAwait(false);

            // Properly make the members fit within an interface if that's what
            // we're generating into.
            if (property.ContainingType.TypeKind == TypeKind.Interface)
            {
                members = members.Select(editor.Generator.AsInterfaceMember)
                                 .WhereNotNull()
                                 .ToImmutableArray();
            }

            var nodeToReplace = service.GetPropertyNodeToReplace(declaration);
            editor.InsertAfter(nodeToReplace, members);
            editor.RemoveNode(nodeToReplace);
        }

        return updatedSolution.WithDocumentSyntaxRoot(documentId, editor.GetChangedRoot());
    }

    private static async Task<ImmutableArray<(IPropertySymbol property, SyntaxNode declaration)>> GetCurrentPropertiesAsync(
        Solution updatedSolution,
        Compilation compilation,
        DocumentId documentId,
        MultiDictionary<DocumentId, IPropertySymbol>.ValueSet originalDefinitions,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<(IPropertySymbol property, SyntaxNode declaration)>.GetInstance(out var result);
        foreach (var originalDefinition in originalDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var property = GetSymbolInCurrentCompilation(compilation, originalDefinition, cancellationToken);
            var declaration = await GetPropertyDeclarationAsync(property, cancellationToken).ConfigureAwait(false);

            if (property != null && declaration != null && updatedSolution.GetDocument(declaration.SyntaxTree)?.Id == documentId)
                result.Add((property, declaration));
        }

        return result.ToImmutableAndClear();
    }

    private static async Task<SyntaxNode?> GetPropertyDeclarationAsync(
        IPropertySymbol? property, CancellationToken cancellationToken)
    {
        if (property == null)
            return null;

        Debug.Assert(property.DeclaringSyntaxReferences.Length == 1);
        var reference = property.DeclaringSyntaxReferences[0];
        return await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TSymbol? GetSymbolInCurrentCompilation<TSymbol>(Compilation compilation, TSymbol originalDefinition, CancellationToken cancellationToken)
        where TSymbol : class, ISymbol
    {
        return originalDefinition.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() as TSymbol;
    }

    public bool Equals((IPropertySymbol property, ReferenceLocation location) x, (IPropertySymbol property, ReferenceLocation location) y)
    {
        Contract.ThrowIfFalse(x.location.Document == y.location.Document);
        return x.location.Location.SourceSpan == y.location.Location.SourceSpan;
    }

    public int GetHashCode((IPropertySymbol property, ReferenceLocation location) obj)
        => obj.location.Location.SourceSpan.GetHashCode();
}
