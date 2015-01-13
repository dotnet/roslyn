// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A context for binding type parameter symbols.
    /// </summary>
    internal sealed class TypeParameterBuilder
    {
        private readonly SyntaxReference syntaxRef;
        private readonly SourceNamedTypeSymbol owner;
        private readonly Location location;

        internal TypeParameterBuilder(SyntaxReference syntaxRef, SourceNamedTypeSymbol owner, Location location)
        {
            this.syntaxRef = syntaxRef;
            Debug.Assert(syntaxRef.GetSyntax().IsKind(SyntaxKind.TypeParameter));
            this.owner = owner;
            this.location = location;
        }

        internal TypeParameterSymbol MakeSymbol(int ordinal, IList<TypeParameterBuilder> builders, DiagnosticBag diagnostics)
        {
            var syntaxNode = (TypeParameterSyntax)this.syntaxRef.GetSyntax();
            var result = new SourceTypeParameterSymbol(
                this.owner,
                syntaxNode.Identifier.ValueText,
                ordinal,
                syntaxNode.VarianceKeyword.VarianceKindFromToken(),
                ToLocations(builders),
                ToSyntaxRefs(builders));

            if (result.Name == result.ContainingSymbol.Name)
            {
                diagnostics.Add(ErrorCode.ERR_TypeVariableSameAsParent, result.Locations[0], result.Name);
            }

            return result;
        }

        private static ImmutableArray<Location> ToLocations(IList<TypeParameterBuilder> builders)
        {
            var arrayBuilder = ArrayBuilder<Location>.GetInstance(builders.Count);
            foreach (var builder in builders)
            {
                arrayBuilder.Add(builder.location);
            }

            return arrayBuilder.ToImmutableAndFree();
        }

        private static ImmutableArray<SyntaxReference> ToSyntaxRefs(IList<TypeParameterBuilder> builders)
        {
            var arrayBuilder = ArrayBuilder<SyntaxReference>.GetInstance(builders.Count);
            foreach (var builder in builders)
            {
                arrayBuilder.Add(builder.syntaxRef);
            }

            return arrayBuilder.ToImmutableAndFree();
        }
    }
}
