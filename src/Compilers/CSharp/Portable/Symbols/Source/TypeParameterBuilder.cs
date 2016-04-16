// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A context for binding type parameter symbols of named types.
    /// </summary>
    internal sealed class TypeParameterBuilder
    {
        private readonly SyntaxReference _syntaxRef;
        private readonly SourceNamedTypeSymbol _owner;
        private readonly Location _location;

        internal TypeParameterBuilder(SyntaxReference syntaxRef, SourceNamedTypeSymbol owner, Location location)
        {
            _syntaxRef = syntaxRef;
            Debug.Assert(syntaxRef.GetSyntax().IsKind(SyntaxKind.TypeParameter));
            _owner = owner;
            _location = location;
        }

        internal TypeParameterSymbol MakeSymbol(int ordinal, IList<TypeParameterBuilder> builders, DiagnosticBag diagnostics)
        {
            var syntaxNode = (TypeParameterSyntax)_syntaxRef.GetSyntax();
            var result = new SourceTypeParameterSymbol(
                _owner,
                syntaxNode.Identifier.ValueText,
                ordinal,
                syntaxNode.VarianceKeyword.VarianceKindFromToken(),
                ToLocations(builders),
                ToSyntaxRefs(builders));

            // SPEC: A type parameter [of a type] cannot have the same name as the type itself.
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
                arrayBuilder.Add(builder._location);
            }

            return arrayBuilder.ToImmutableAndFree();
        }

        private static ImmutableArray<SyntaxReference> ToSyntaxRefs(IList<TypeParameterBuilder> builders)
        {
            var arrayBuilder = ArrayBuilder<SyntaxReference>.GetInstance(builders.Count);
            foreach (var builder in builders)
            {
                arrayBuilder.Add(builder._syntaxRef);
            }

            return arrayBuilder.ToImmutableAndFree();
        }
    }
}
