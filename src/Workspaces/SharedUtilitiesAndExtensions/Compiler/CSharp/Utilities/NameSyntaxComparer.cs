// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class NameSyntaxComparer : IComparer<NameSyntax?>
    {
        private readonly IComparer<SyntaxToken> _tokenComparer;
        internal readonly TypeSyntaxComparer TypeComparer;

        internal NameSyntaxComparer(IComparer<SyntaxToken> tokenComparer)
        {
            _tokenComparer = tokenComparer;
            TypeComparer = new TypeSyntaxComparer(tokenComparer, this);
        }

        public static IComparer<NameSyntax?> Create()
            => Create(TokenComparer.NormalInstance);

        public static IComparer<NameSyntax?> Create(IComparer<SyntaxToken> tokenComparer)
            => new NameSyntaxComparer(tokenComparer);

        public int Compare(NameSyntax? x, NameSyntax? y)
        {
            if (x == y)
                return 0;

            return (x, y) switch
            {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => 1,
                ({ IsMissing: true }, { IsMissing: true }) => 0,
                ({ IsMissing: true }, _) => -1,
                (_, { IsMissing: true }) => 1,
                (IdentifierNameSyntax identifierX, IdentifierNameSyntax identifierY) => _tokenComparer.Compare(identifierX.Identifier, identifierY.Identifier),
                (GenericNameSyntax genericX, GenericNameSyntax genericY) => Compare(genericX, genericY),
                (IdentifierNameSyntax identifierX, GenericNameSyntax genericY) =>
                    _tokenComparer.Compare(identifierX.Identifier, genericY.Identifier) is var diff && diff != 0
                        ? diff
                        : -1, // Goo goes before Goo<T>
                (GenericNameSyntax genericX, IdentifierNameSyntax identifierY) =>
                    _tokenComparer.Compare(genericX.Identifier, identifierY.Identifier) is var diff && diff != 0
                        ? diff
                        : -1, // Goo<T> goes after Goo
                (_, _) => DecomposeCompare(x, y),
            };

            int DecomposeCompare(NameSyntax x, NameSyntax y)
            {
                // At this point one or both of the nodes is a dotted name or
                // aliased name.  Break them apart into individual pieces and
                // compare those.

                var xNameParts = DecomposeNameParts(x);
                var yNameParts = DecomposeNameParts(y);

                for (var i = 0; i < xNameParts.Count && i < yNameParts.Count; i++)
                {
                    var compare = Compare(xNameParts[i], yNameParts[i]);
                    if (compare != 0)
                        return compare;
                }

                // they matched up to this point.  The shorter one should come
                // first.
                return xNameParts.Count - yNameParts.Count;
            }
        }

        private static IList<SimpleNameSyntax> DecomposeNameParts(NameSyntax name)
        {
            var result = new List<SimpleNameSyntax>();
            DecomposeNameParts(name, result);
            return result;
        }

        private static void DecomposeNameParts(NameSyntax name, List<SimpleNameSyntax> result)
        {
            switch (name.Kind())
            {
                case SyntaxKind.QualifiedName:
                    var dottedName = (QualifiedNameSyntax)name;
                    DecomposeNameParts(dottedName.Left, result);
                    DecomposeNameParts(dottedName.Right, result);
                    break;
                case SyntaxKind.AliasQualifiedName:
                    var aliasedName = (AliasQualifiedNameSyntax)name;
                    result.Add(aliasedName.Alias);
                    DecomposeNameParts(aliasedName.Name, result);
                    break;
                case SyntaxKind.IdentifierName:
                    result.Add((IdentifierNameSyntax)name);
                    break;
                case SyntaxKind.GenericName:
                    result.Add((GenericNameSyntax)name);
                    break;
            }
        }

        private int Compare(GenericNameSyntax x, GenericNameSyntax y)
        {
            var compare = _tokenComparer.Compare(x.Identifier, y.Identifier);
            if (compare != 0)
                return compare;

            // The one with less type params comes first.
            compare = x.Arity - y.Arity;
            if (compare != 0)
                return compare;

            // Same name, same parameter count.  Compare each parameter.
            for (var i = 0; i < x.Arity; i++)
            {
                var xArg = x.TypeArgumentList.Arguments[i];
                var yArg = y.TypeArgumentList.Arguments[i];

                compare = TypeComparer.Compare(xArg, yArg);
                if (compare != 0)
                    return compare;
            }

            return 0;
        }
    }
}
