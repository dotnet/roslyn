// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.MakeTypeClosed;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeTypeClosed), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeTypeClosedCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [
            "CS8509", // warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{some pattern}' is not covered.
        ];

    private static async Task<TypeDeclarationSyntax?> GetTypeToRefactorWhenValidAsync(
        Solution solution,
        SemanticModel semanticModel,
        SyntaxNode? node,
        CancellationToken cancellationToken = default)
    {
        var switchExpression = node?.FirstAncestorOrSelf<SwitchExpressionSyntax>();
        if (switchExpression == null)
            return null;

        var switchedOnType = semanticModel.GetTypeInfo(switchExpression.GoverningExpression, cancellationToken: cancellationToken).Type;

        if (switchedOnType is not INamedTypeSymbol namedType)
            return null;

        if (namedType.TypeKind != TypeKind.Class)
            return null;

        if (namedType.IsStatic)
            return null;

        if (namedType.IsSealed)
            return null;

        if (namedType.IsClosed)
            return null;

        var declaration = namedType.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaration == null)
            return null;

        if (await declaration.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is not TypeDeclarationSyntax syntax)
            return null;

        if (namedType.DeclaredAccessibility == Accessibility.Private)
            return syntax;

        var switchArmTypes = switchExpression.DescendantNodes()
            .OfType<SwitchExpressionArmSyntax>()
            .Select(arm => arm.Pattern)
            .OfType<DeclarationPatternSyntax>();

        var namedTypeAllowsNulls = namedType.NullableAnnotation == NullableAnnotation.Annotated;
        // TODO check if there is a null arm if the above is true

        if (!await AreAllDerrivedTypesHandledAsync(solution, semanticModel, switchExpression.Arms, namedType, true, cancellationToken).ConfigureAwait(false))
            return null;

        if (!namedType.IsAbstract && await IsTypeEverInstantiatedAsync(solution, namedType, cancellationToken).ConfigureAwait(false))
            return null;

        return syntax;
    }

    private static async Task<bool> AreAllDerrivedTypesHandledAsync(
        Solution solution,
        SemanticModel semanticModel,
        SeparatedSyntaxList<SwitchExpressionArmSyntax> switchArms,
        INamedTypeSymbol type,
        bool hasToBeInSameAssembly = true,
        CancellationToken cancellationToken = default)
    {
        var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(type, solution, false, [.. solution.Projects], cancellationToken).ConfigureAwait(false);
        foreach (var derivedClass in derivedClasses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (hasToBeInSameAssembly && !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, derivedClass.ContainingAssembly))
                return false;

            var isHandled = switchArms.Any(arm =>
            {
                var patternType = semanticModel.GetTypeInfo(arm.Pattern, cancellationToken: cancellationToken).Type;
                return SymbolEqualityComparer.Default.Equals(patternType, derivedClass);
            });
            if (isHandled)
                continue;

            if (!await AreAllDerrivedTypesHandledAsync(solution, semanticModel, switchArms, derivedClass, false, cancellationToken).ConfigureAwait(false))
                return false;
        }

        return true;
    }

    private static async Task<bool> IsTypeEverInstantiatedAsync(
        Solution solution,
        INamedTypeSymbol type,
        CancellationToken cancellationToken = default)
    {
        var references = await SymbolFinder.FindReferencesAsync(type, solution, cancellationToken).ConfigureAwait(false);
        foreach (var reference in references)
        {
            foreach (var location in reference.Locations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!location.Location.IsInSource)
                    continue;

                var document = solution.GetDocument(location.Location.SourceTree);
                if (document == null)
                    continue;

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(root);

                var node = root.FindNode(location.Location.SourceSpan);

                var objectCreation = node.FirstAncestorOrSelf<BaseObjectCreationExpressionSyntax>();
                if (objectCreation != null)
                {
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var createdType = model.GetTypeInfo(objectCreation, cancellationToken: cancellationToken).Type;
                    if (SymbolEqualityComparer.Default.Equals(createdType, type))
                        return true;
                }
            }
        }

        return false;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        Contract.ThrowIfNull(document);

        var diagnostic = context.Diagnostics[0];
        var diagTree = diagnostic.Location.SourceTree;
        var diagDocument = diagTree != null ? document.Project.Solution.GetDocument(diagTree) : document;
        if (diagDocument == null)
            return;

        var semanticModel = await diagDocument.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(semanticModel);

        var root = await diagDocument.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(root);

        var node = root.FindNode(diagnostic.Location.SourceSpan);

        var typeDecl = await GetTypeToRefactorWhenValidAsync(document.Project.Solution, semanticModel, node, context.CancellationToken).ConfigureAwait(false);

        if (typeDecl == null)
            return;

        async Task<Solution> ApplyAsync(CancellationToken ct)
        {
            var targetDoc = document.Project.Solution.GetDocument(typeDecl.SyntaxTree);
            if (targetDoc == null)
                return document.Project.Solution;

            var targetRoot = await targetDoc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            Contract.ThrowIfNull(targetRoot);

            var generator = SyntaxGenerator.GetGenerator(targetDoc);
            var newNode = generator.WithModifiers(
                typeDecl,
                generator.GetModifiers(typeDecl)
                    .WithIsAbstract(false)
                    .WithIsClosed(true));
            var newRoot = targetRoot.ReplaceNode(typeDecl, newNode);

            return targetDoc.Project.Solution.WithDocumentSyntaxRoot(targetDoc.Id, newRoot);
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                CodeFixesResources.Make_class_closed,
                ApplyAsync,
                CodeFixesResources.Make_class_closed),
            context.Diagnostics);
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(semanticModel);

        foreach (var diagnostic in diagnostics)
        {
            var diagTree = diagnostic.Location.SourceTree;
            var diagDocument = diagTree != null ? document.Project.Solution.GetDocument(diagTree) : document;
            if (diagDocument == null)
                continue;

            var diagSemanticModel = await diagDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(diagSemanticModel);

            var diagRoot = await diagDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(diagRoot);

            var node = diagRoot.FindNode(diagnostic.Location.SourceSpan);

            var typeDeclaration = await GetTypeToRefactorWhenValidAsync(
                    document.Project.Solution,
                    diagSemanticModel,
                    node,
                    cancellationToken)
                .ConfigureAwait(false);

            if (typeDeclaration != null)
            {
                var targetDoc = document.Project.Solution.GetDocument(typeDeclaration.SyntaxTree);
                if (targetDoc != null && targetDoc.Id == document.Id)
                {
                    editor.ReplaceNode(typeDeclaration,
                        (currentTypeDeclaration, generator) =>
                            generator.WithModifiers(
                                currentTypeDeclaration,
                                generator.GetModifiers(currentTypeDeclaration)
                                    .WithIsAbstract(false)
                                    .WithIsClosed(true)
                            )
                    );
                }
            }
        }
    }
}
