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
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

using static UseAutoPropertiesHelpers;

internal abstract partial class AbstractUseAutoPropertyCodeFixProvider<
    TProvider,
    TTypeDeclarationSyntax,
    TPropertyDeclaration,
    TVariableDeclarator,
    TConstructorDeclaration,
    TExpression>
    : CodeFixProvider
    where TProvider : AbstractUseAutoPropertyCodeFixProvider<
        TProvider,
        TTypeDeclarationSyntax,
        TPropertyDeclaration,
        TVariableDeclarator,
        TConstructorDeclaration,
        TExpression>
    where TTypeDeclarationSyntax : SyntaxNode
    where TPropertyDeclaration : SyntaxNode
    where TVariableDeclarator : SyntaxNode
    where TConstructorDeclaration : SyntaxNode
    where TExpression : SyntaxNode
{
    protected static SyntaxAnnotation SpecializedFormattingAnnotation = new();

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseAutoPropertyDiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => new UseAutoPropertyFixAllProvider((TProvider)this);

    protected abstract ISyntaxFormatting SyntaxFormatting { get; }

    protected abstract TPropertyDeclaration GetPropertyDeclaration(SyntaxNode node);
    protected abstract SyntaxNode GetNodeToRemove(TVariableDeclarator declarator);
    protected abstract TPropertyDeclaration RewriteFieldReferencesInProperty(
        TPropertyDeclaration property, ImmutableArray<ReferencedSymbol> fieldLocations, CancellationToken cancellationToken);

    protected abstract ImmutableArray<AbstractFormattingRule> GetFormattingRules(
        Document document, SyntaxNode finalPropertyDeclaration);

    protected abstract Task<SyntaxNode> UpdatePropertyAsync(
        Document propertyDocument,
        Compilation compilation,
        IFieldSymbol fieldSymbol,
        IPropertySymbol propertySymbol,
        TVariableDeclarator fieldDeclarator,
        TPropertyDeclaration propertyDeclaration,
        bool isWrittenOutsideConstructor,
        bool isTrivialGetAccessor,
        bool isTrivialSetAccessor,
        CancellationToken cancellationToken);

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var solution = context.Document.Project.Solution;

        foreach (var diagnostic in context.Diagnostics)
        {
            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Default;

            context.RegisterCodeFix(CodeAction.Create(
                    AnalyzersResources.Use_auto_property,
                    cancellationToken => ProcessResultAsync(solution, solution, diagnostic, cancellationToken),
                    equivalenceKey: nameof(AnalyzersResources.Use_auto_property),
                    priority),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private async Task<Solution> ProcessResultAsync(
        Solution originalSolution, Solution currentSolution, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        try
        {
            return await ProcessResultWorkerAsync(originalSolution, currentSolution, diagnostic, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
        {
            return currentSolution;
        }
    }

    private async Task<Solution> ProcessResultWorkerAsync(
        Solution originalSolution, Solution currentSolution, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var (field, property) = await MapDiagnosticToCurrentSolutionAsync(
            diagnostic, originalSolution, currentSolution, cancellationToken).ConfigureAwait(false);

        if (field == null || property == null)
            return currentSolution;

        var fieldDocument = currentSolution.GetRequiredDocument(field.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).SyntaxTree);
        var propertyDocument = currentSolution.GetRequiredDocument(property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).SyntaxTree);

        var isTrivialGetAccessor = diagnostic.Properties.ContainsKey(IsTrivialGetAccessor);
        var isTrivialSetAccessor = diagnostic.Properties.ContainsKey(IsTrivialSetAccessor);

        Debug.Assert(fieldDocument.Project == propertyDocument.Project);
        var project = fieldDocument.Project;
        var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        var fieldLocations = (await SymbolFinder.FindReferencesAsync(
            field, currentSolution, cancellationToken).ConfigureAwait(false)).ToImmutableArray();

        var declarator = (TVariableDeclarator)field.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        var propertyDeclaration = GetPropertyDeclaration(property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken));

        // First, create the updated property we want to replace the old property with
        var (isWrittenToOutsideOfConstructor, isWrittenNull) = await AnalyzeFieldWritesAsync(
            fieldDocument.GetRequiredLanguageService<ISyntaxFactsService>(),
            field, fieldLocations, propertyDeclaration, cancellationToken).ConfigureAwait(false);

        if (!isTrivialGetAccessor ||
            (property.SetMethod != null && !isTrivialSetAccessor))
        {
            // We have at least a non-trivial getter/setter.  Those will not be rewritten to `get;/set;`.  As such, we
            // need to update the property to reference `field` or itself instead of the actual field.
            propertyDeclaration = RewriteFieldReferencesInProperty(propertyDeclaration, fieldLocations, cancellationToken);
        }

        var updatedProperty = await UpdatePropertyAsync(
            propertyDocument, compilation,
            field, property,
            declarator, propertyDeclaration,
            isWrittenToOutsideOfConstructor,
            isTrivialGetAccessor, isTrivialSetAccessor,
            cancellationToken).ConfigureAwait(false);

        // We're replacing a nullable field with a non-nullable property.  If something nullable is ever written into
        // the field, then we need to add [AllowNull] to the property to allow that same code to continue to work.
        if (isWrittenNull &&
            field.Type.NullableAnnotation is NullableAnnotation.Annotated &&
            property.Type.NullableAnnotation is NullableAnnotation.NotAnnotated)
        {
            var allowNullAttribute = compilation.AllowNullAttribute();
            if (allowNullAttribute != null)
            {
                var generator = propertyDocument.GetRequiredLanguageService<SyntaxGenerator>();
                updatedProperty = generator.AddAttributes(
                    updatedProperty,
                    generator.Attribute(generator.TypeExpression(allowNullAttribute, addImport: true)));
            }
        }

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

        // Now, rename all usages of the field to point at the property.
        currentSolution = await UpdateReferencesAsync(
             currentSolution, linkedFiles, fieldLocations, property, cancellationToken).ConfigureAwait(false);

        // Now find the field and property again post rename.
        fieldDocument = currentSolution.GetRequiredDocument(fieldDocument.Id);
        propertyDocument = currentSolution.GetRequiredDocument(propertyDocument.Id);
        Debug.Assert(fieldDocument.Project == propertyDocument.Project);

        compilation = await fieldDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        field = (IFieldSymbol?)field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol;
        property = (IPropertySymbol?)property.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol;
        Contract.ThrowIfTrue(field == null || property == null);

        declarator = (TVariableDeclarator)field.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        propertyDeclaration = GetPropertyDeclaration(property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken));

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
        var fieldDocumentSyntaxFacts = fieldDocument.GetRequiredLanguageService<ISyntaxFactsService>();
        if (fieldDocument == propertyDocument)
        {
            var bannerService = fieldDocument.GetRequiredLanguageService<IFileBannerFactsService>();
            if (WillRemoveFirstFieldInTypeDirectlyAboveProperty(fieldDocumentSyntaxFacts, propertyDeclaration, nodeToRemove) &&
                bannerService.GetLeadingBlankLines(nodeToRemove).Length == 0)
            {
                updatedProperty = bannerService.GetNodeWithoutLeadingBlankLines(updatedProperty);
            }
        }

        var syntaxRemoveOptions = CreateSyntaxRemoveOptions(fieldDocumentSyntaxFacts, nodeToRemove);
        if (fieldDocument == propertyDocument)
        {
            // Same file.  Have to do this in a slightly complicated fashion.
            var declaratorTreeRoot = await fieldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(declaratorTreeRoot, fieldDocument.Project.Solution.Services);
            editor.ReplaceNode(propertyDeclaration, updatedProperty);
            editor.RemoveNode(nodeToRemove, syntaxRemoveOptions);

            var updatedFieldDocument = fieldDocument.WithSyntaxRoot(editor.GetChangedRoot());
            var finalFieldRoot = await FormatAsync(updatedFieldDocument, updatedProperty, cancellationToken).ConfigureAwait(false);

            return currentSolution.WithDocumentSyntaxRoot(fieldDocument.Id, finalFieldRoot);
        }
        else
        {
            // In different files.  Just update both files.
            var fieldTreeRoot = await fieldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var propertyTreeRoot = await propertyDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newFieldTreeRoot = fieldTreeRoot.RemoveNode(nodeToRemove, syntaxRemoveOptions);
            Contract.ThrowIfNull(newFieldTreeRoot);
            var newPropertyTreeRoot = propertyTreeRoot.ReplaceNode(propertyDeclaration, updatedProperty);

            var updatedFieldDocument = fieldDocument.WithSyntaxRoot(newFieldTreeRoot);
            var updatedPropertyDocument = propertyDocument.WithSyntaxRoot(newPropertyTreeRoot);

            newFieldTreeRoot = await FormatAsync(updatedFieldDocument, updatedProperty, cancellationToken).ConfigureAwait(false);
            newPropertyTreeRoot = await FormatAsync(updatedPropertyDocument, updatedProperty, cancellationToken).ConfigureAwait(false);

            var updatedSolution = currentSolution.WithDocumentSyntaxRoot(fieldDocument.Id, newFieldTreeRoot);
            updatedSolution = updatedSolution.WithDocumentSyntaxRoot(propertyDocument.Id, newPropertyTreeRoot);

            return updatedSolution;
        }
    }

    private static async Task<Solution> UpdateReferencesAsync(
        Solution solution,
        HashSet<DocumentId> linkedDocuments,
        ImmutableArray<ReferencedSymbol> fieldLocations,
        IPropertySymbol property,
        CancellationToken cancellationToken)
    {
        var solutionEditor = new SolutionEditor(solution);
        var canEditMap = new Dictionary<DocumentId, bool>();

        foreach (var group in fieldLocations.SelectMany(loc => loc.Locations).GroupBy(loc => loc.Document))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = group.Key;

            if (!CanEditDocument(document.Id))
                continue;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var editor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var generator = editor.Generator;
            var newNameNode = generator.IdentifierName(property.Name);

            foreach (var location in group.Distinct(LinkedFileReferenceLocationEqualityComparer.Instance))
            {
                if (location.IsImplicit)
                    continue;

                var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                if (syntaxFacts.GetRootStandaloneExpression(node) == node)
                {
                    // We're referencing the field as a trivial name (like `fieldName`).  In this case, we might run into
                    // problems with symbol collisions if we just change the name to `propertyName`.  So instead, we check
                    // if that name is in scope and isn't a reference to the new property.  If that's the case, then we
                    // qualify with `this.fieldName` or `ClassName.FieldName` to avoid any collisions.
                    var symbols = semanticModel.LookupSymbols(node.SpanStart, name: property.Name);
                    if (symbols.Length > 0 && symbols.All(s => !s.OriginalDefinition.Equals(property.OriginalDefinition)))
                    {
                        var qualifiedName = generator.MemberAccessExpression(
                            property.IsStatic ? generator.TypeExpression(property.ContainingType) : generator.ThisExpression(),
                            newNameNode);
                        editor.ReplaceNode(node, qualifiedName.WithTriviaFrom(node));
                    }
                    else
                    {
                        // The name was standing alone and didn't bind to any other symbol.  Just do the trivial rename here.
                        editor.ReplaceNode(node, newNameNode.WithTriviaFrom(node));
                    }
                }
                else
                {
                    // Otherwise, we're referencing the field in a complex way (like `this.fieldName`).  In this case, we can just
                    // trivially replace `fieldName` with `propertyName` and have it work.  Note: we add the simplifier annotation
                    // here as well.  That way we can attempt to simplify the code if the user does not prefer `this` qualifiers
                    // for properties.
                    editor.ReplaceNode(node, newNameNode.WithTriviaFrom(node));
                    editor.ReplaceNode(node.GetRequiredParent(), (current, _) => current.WithAdditionalAnnotations(Simplifier.Annotation));
                }
            }
        }

        return solutionEditor.GetChangedSolution();

        bool CanEditDocument(DocumentId documentId)
        {
            if (!canEditMap.TryGetValue(documentId, out var canEditDocument))
            {
                var document = solution.GetDocument(documentId);
                canEditDocument = document != null && !linkedDocuments.Contains(document.Id);
                canEditMap[documentId] = canEditDocument;
            }

            return canEditDocument;
        }
    }

    private async Task<(IFieldSymbol? fieldSymbol, IPropertySymbol? propertySymbol)> MapDiagnosticToCurrentSolutionAsync(
        Diagnostic diagnostic,
        Solution originalSolution,
        Solution currentSolution,
        CancellationToken cancellationToken)
    {
        var locations = diagnostic.AdditionalLocations;

        var propertyLocation = locations[0];
        var declaratorLocation = locations[1];

        // Look up everything in the original solution.

        var declarator = (TVariableDeclarator)declaratorLocation.FindNode(cancellationToken);
        var fieldDocument = originalSolution.GetRequiredDocument(declarator.SyntaxTree);
        var fieldSemanticModel = await fieldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var fieldSymbol = (IFieldSymbol)fieldSemanticModel.GetRequiredDeclaredSymbol(declarator, cancellationToken);

        var property = GetPropertyDeclaration(propertyLocation.FindNode(cancellationToken));
        var propertyDocument = originalSolution.GetRequiredDocument(property.SyntaxTree);
        var propertySemanticModel = await propertyDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var propertySymbol = (IPropertySymbol)propertySemanticModel.GetRequiredDeclaredSymbol(property, cancellationToken);

        Contract.ThrowIfFalse(fieldDocument.Project == propertyDocument.Project);

        // If we're just starting, no need to map anything.
        if (originalSolution != currentSolution)
        {
            var currentProject = currentSolution.GetRequiredProject(fieldDocument.Project.Id);
            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            fieldSymbol = fieldSymbol.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol() as IFieldSymbol;
            propertySymbol = propertySymbol.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol() as IPropertySymbol;
        }

        return (fieldSymbol, propertySymbol);
    }

    private static SyntaxRemoveOptions CreateSyntaxRemoveOptions(
        ISyntaxFacts syntaxFacts, SyntaxNode nodeToRemove)
    {
        var syntaxRemoveOptions = SyntaxGenerator.DefaultRemoveOptions;

        if (nodeToRemove.GetLeadingTrivia().Any(t => t.IsDirective || syntaxFacts.IsRegularComment(t)))
            syntaxRemoveOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;

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

#pragma warning disable CA1822 // Mark members as static
    private async Task<SyntaxNode> FormatAsync(
        Document document,
        SyntaxNode finalPropertyDeclaration,
        CancellationToken cancellationToken)
    {
        // First see if we need to apply any specialized formatting rules.
        var formattingRules = GetFormattingRules(document, finalPropertyDeclaration);
        if (!formattingRules.IsDefault)
        {
            var options = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
            document = await this.SyntaxFormatting.FormatAsync(
                document, SpecializedFormattingAnnotation, options, formattingRules, cancellationToken).ConfigureAwait(false);
        }

        var codeCleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
        var cleanedDocument = await CodeCleanupHelpers.CleanupSyntaxAsync(
            document, codeCleanupOptions, cancellationToken).ConfigureAwait(false);

        return await cleanedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore CA1822 // Mark members as static

    private static async ValueTask<(bool isWrittenOutsideConstructor, bool isWrittenNull)> AnalyzeFieldWritesAsync(
        ISyntaxFactsService syntaxFacts,
        IFieldSymbol field,
        ImmutableArray<ReferencedSymbol> referencedSymbols,
        TPropertyDeclaration propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var isWrittenOutsideConstructor = false;
        var isWrittenNull = false;
        var constructorSpans = field.ContainingType
            .GetMembers()
            .Where(m => m.IsConstructor())
            .SelectMany(c => c.DeclaringSyntaxReferences)
            .Select(s => s.GetSyntax(cancellationToken))
            .Select(n => n.FirstAncestorOrSelf<TConstructorDeclaration>())
            .WhereNotNull()
            .Select(d => (d.SyntaxTree.FilePath, d.Span))
            .ToSet();

        foreach (var group in referencedSymbols.SelectMany(r => r.Locations).GroupBy(loc => loc.Document))
        {
            var document = group.Key;
            var lazySemanticModel = AsyncLazy.Create(
                async cancellationToken => await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false));

            foreach (var location in group)
            {
                isWrittenOutsideConstructor = isWrittenOutsideConstructor ||
                    await IsWrittenToOutsideOfConstructorOrPropertyAsync(lazySemanticModel, location, propertyDeclaration, constructorSpans, cancellationToken).ConfigureAwait(false);
                isWrittenNull = isWrittenNull ||
                    await IsWrittenNullAsync(syntaxFacts, lazySemanticModel, location, cancellationToken).ConfigureAwait(false);

                if (isWrittenOutsideConstructor && isWrittenNull)
                    return (true, true);
            }
        }

        return (isWrittenOutsideConstructor, isWrittenNull);
    }

    private static async ValueTask<bool> IsWrittenNullAsync(
        ISyntaxFactsService syntaxFacts,
        AsyncLazy<SemanticModel> lazySemanticModel,
        ReferenceLocation location,
        CancellationToken cancellationToken)
    {
#if !CODE_STYLE
        if (!location.IsWrittenTo)
            return false;
#endif

        if (location.IsImplicit)
            return false;

        var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
        if (syntaxFacts.IsNameOfAnyMemberAccessExpression(node))
            node = node.GetRequiredParent();

        if (!syntaxFacts.IsLeftSideOfAnyAssignment(node))
            return false;

        var assignment = node.GetRequiredParent();
        var rightSide = syntaxFacts.GetRightHandSideOfAssignment(assignment);

        var semanticModel = await lazySemanticModel.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var typeInfo = semanticModel.GetTypeInfo(rightSide, cancellationToken);

        return typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull;
    }

    private static async ValueTask<bool> IsWrittenToOutsideOfConstructorOrPropertyAsync(
        AsyncLazy<SemanticModel> lazySemanticModel,
        ReferenceLocation location,
        TPropertyDeclaration propertyDeclaration,
        ISet<(string filePath, TextSpan span)> constructorSpans,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (location.IsImplicit)
            return false;

        // We don't need a setter if we're not writing to this field.
        if (!await IsWrittenToAsync(location).ConfigureAwait(false))
            return false;

        var syntaxFacts = location.Document.GetRequiredLanguageService<ISyntaxFactsService>();

        var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
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

        // Remove after .NET 10, https://github.com/dotnet/roslyn/issues/80198
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async ValueTask<bool> IsWrittenToAsync(ReferenceLocation loc)
        {
#if !CODE_STYLE
            return loc.IsWrittenTo;
#else
            var semanticFacts = loc.Document.GetRequiredLanguageService<ISemanticFactsService>();
            var semanticModel = await lazySemanticModel.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var usage = SymbolUsageInfo.GetSymbolUsageInfo(
                semanticFacts, semanticModel, loc.Location.FindNode(getInnermostNodeForTie: true, cancellationToken), cancellationToken);
            return usage.IsWrittenTo();
#endif
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
