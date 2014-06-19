// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal static class CSSyntaxExtensions
    {
        internal static string SimpleName(this TypeSyntax syntax)
        {
            if (syntax is NameSyntax)
            {
                return (syntax as NameSyntax).SimpleName();
            }

            if (syntax is PredefinedTypeSyntax)
            {
                return (syntax as PredefinedTypeSyntax).Keyword.GetText();
            }

            throw new NotImplementedException();
        }

        internal static string SimpleName(this NameSyntax syntax)
        {
            while (true)
            {
                var simple = syntax as IdentifierNameSyntax;
                if (simple != null)
                {
                    return simple.GetText();
                }

                var generic = syntax as GenericNameSyntax;
                if (generic != null)
                {
                    return generic.Identifier.GetText();
                }

                var aliased = syntax as AliasQualifiedNameSyntax;
                if (aliased != null)
                {
                    return aliased.Identifier.GetText();
                }

                var qualified = syntax as QualifiedNameSyntax;
                if (qualified != null)
                {
                    syntax = qualified.Right;
                    continue;
                }

                throw new NotImplementedException();
            }
        }

        internal static IEnumerable<string> GetNamesFromLeft(this NameSyntax name)
        {
            return GetNamesFromRight(name).Reverse();
        }

        internal static IEnumerable<string> GetNamesFromRight(this NameSyntax name)
        {
            while (true)
            {
                if (name is QualifiedNameSyntax)
                {
                    yield return ((QualifiedNameSyntax)name).Right.GetText();
                    name = ((QualifiedNameSyntax)name).Left;
                }
                else
                {
                    yield return name.SimpleName();
                    yield break;
                }
            }
        }

        internal static int GetArity(this NameSyntax name)
        {
            if (name is GenericNameSyntax)
            {
                return ((GenericNameSyntax)name).Arguments.Count;
            }

            return 0;
        }
    }
}