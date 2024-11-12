// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editing;

public static class SymbolEditorExtensions
{
    /// <summary>
    /// Gets the reference to the declaration of the base or interface type as part of the symbol's declaration. 
    /// </summary>
    public static async Task<SyntaxNode> GetBaseOrInterfaceDeclarationReferenceAsync(
        this SymbolEditor editor,
        ISymbol symbol,
        ITypeSymbol baseOrInterfaceType,
        CancellationToken cancellationToken = default)
    {
        if (baseOrInterfaceType == null)
        {
            throw new ArgumentNullException(nameof(baseOrInterfaceType));
        }

        if (baseOrInterfaceType.TypeKind != TypeKind.Error)
        {
            baseOrInterfaceType = (ITypeSymbol)(await editor.GetCurrentSymbolAsync(baseOrInterfaceType, cancellationToken).ConfigureAwait(false));
        }

        // look for the base or interface declaration in all declarations of the symbol
        var currentDecls = await editor.GetCurrentDeclarationsAsync(symbol, cancellationToken).ConfigureAwait(false);

        foreach (var decl in currentDecls)
        {
            var doc = editor.OriginalSolution.GetDocument(decl.SyntaxTree);
            var model = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var gen = SyntaxGenerator.GetGenerator(doc);

            var typeRef = gen.GetBaseAndInterfaceTypes(decl).FirstOrDefault(r => model.GetTypeInfo(r, cancellationToken).Type.Equals(baseOrInterfaceType));
            if (typeRef != null)
            {
                return typeRef;
            }
        }

        return null;
    }

    /// <summary>
    /// Changes the base type of the symbol.
    /// </summary>
    public static async Task<ISymbol> SetBaseTypeAsync(
        this SymbolEditor editor,
        INamedTypeSymbol symbol,
        Func<SyntaxGenerator, SyntaxNode> getNewBaseType,
        CancellationToken cancellationToken = default)
    {
        var baseType = symbol.BaseType;

        if (baseType != null)
        {
            // find existing declaration of the base type
            var typeRef = await editor.GetBaseOrInterfaceDeclarationReferenceAsync(symbol, baseType, cancellationToken).ConfigureAwait(false);
            if (typeRef != null)
            {
                return await editor.EditOneDeclarationAsync(
                    symbol,
                    typeRef.GetLocation(),
                    (e, d) => e.ReplaceNode(typeRef, getNewBaseType(e.Generator)),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        // couldn't find the existing reference to change, so add it to one of the declarations
        return await editor.EditOneDeclarationAsync(symbol, (e, decl) =>
        {
            var newBaseType = getNewBaseType(e.Generator);
            if (newBaseType != null)
            {
                e.ReplaceNode(decl, (d, g) => g.AddBaseType(d, newBaseType));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Changes the base type of the symbol.
    /// </summary>
    public static Task<ISymbol> SetBaseTypeAsync(
        this SymbolEditor editor,
        INamedTypeSymbol symbol,
        ITypeSymbol newBaseType,
        CancellationToken cancellationToken = default)
    {
        return editor.SetBaseTypeAsync(symbol, g => newBaseType != null ? g.TypeExpression(newBaseType) : null, cancellationToken);
    }
}
