// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeTypeClosed;

internal abstract class AbstractMakeTypeClosedCodeFixProvider<TTypeDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
    where TTypeDeclarationSyntax : SyntaxNode
{
    protected abstract Task<(bool, TTypeDeclarationSyntax?)> GetRefactorContextValidityAndTypeAsync(SyntaxNode? node, SemanticModel semanticModel, Solution solution, CancellationToken cancellationToken);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        Contract.ThrowIfNull(document);

        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(semanticModel);

        var (isValid, _) = await GetRefactorContextValidityAndTypeAsync(
            context.Diagnostics[0].Location?.FindNode(context.CancellationToken),
            semanticModel,
            document.Project.Solution,
            context.CancellationToken
            ).ConfigureAwait(false);
        if (isValid)
        {
            RegisterCodeFix(context, CodeFixesResources.Make_class_closed, CodeFixesResources.Make_class_closed);
        }
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(semanticModel);

        foreach (var diagnostic in diagnostics)
        {
            if (await (
                    GetRefactorContextValidityAndTypeAsync(
                        diagnostic.Location?.FindNode(cancellationToken),
                        semanticModel,
                        document.Project.Solution,
                        cancellationToken)
                        .ConfigureAwait(false)
                ) is (true, var typeDeclaration)
            )
            {
                if (typeDeclaration == null)
                    continue; // TODO: consider throwing an exception here, since we should never get a null typeDeclaration.

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

    protected static async Task<bool> HasExternalDerivedTypesAsync(INamedTypeSymbol baseType, Solution solution, CancellationToken cancellationToken)
    {
        // Precheck for sealed, private or closed types, which cannot have external derived types.
        // I'm assuming the prosessing of the entire compilation to find derived types is expensive, so we want to avoid it if possible.
        if (baseType.IsSealed || baseType.IsClosed || baseType.DeclaredAccessibility == Accessibility.Private)
            return false;

        var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(baseType, solution, solution.Projects.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);
        foreach (var derivedClass in derivedClasses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, derivedClass.ContainingAssembly))
                return true;
        }

        return false;
    }
}
