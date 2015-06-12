// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    internal partial class MemberDeclarationsOrganizer
    {
        private class Comparer : IComparer<MemberDeclarationSyntax>
        {
            // TODO(cyrusn): Allow users to specify the ordering they want
            private enum OuterOrdering
            {
                Fields,
                EventFields,
                Constructors,
                Destructors,
                Properties,
                Events,
                Indexers,
                Operators,
                ConversionOperators,
                Methods,
                Types,
                Remaining
            }

            private enum InnerOrdering
            {
                StaticInstance,
                Accessibility,
                Name
            }

            private enum Accessibility
            {
                Public,
                Protected,
                ProtectedOrInternal,
                Internal,
                Private
            }

            public int Compare(MemberDeclarationSyntax x, MemberDeclarationSyntax y)
            {
                if (x == y)
                {
                    return 0;
                }

                var xOuterOrdering = GetOuterOrdering(x);
                var yOuterOrdering = GetOuterOrdering(y);

                var compare = xOuterOrdering - yOuterOrdering;
                if (compare != 0)
                {
                    return compare;
                }

                if (xOuterOrdering == OuterOrdering.Remaining)
                {
                    return 1;
                }
                else if (yOuterOrdering == OuterOrdering.Remaining)
                {
                    return -1;
                }

                if (xOuterOrdering == OuterOrdering.Fields || yOuterOrdering == OuterOrdering.Fields)
                {
                    // Fields with initializers can't be reordered relative to 
                    // themselves due to ordering issues.
                    var xHasInitializer = ((FieldDeclarationSyntax)x).Declaration.Variables.Any(v => v.Initializer != null);
                    var yHasInitializer = ((FieldDeclarationSyntax)y).Declaration.Variables.Any(v => v.Initializer != null);
                    if (xHasInitializer && yHasInitializer)
                    {
                        return 0;
                    }
                }

                var xIsStatic = x.GetModifiers().Any(t => t.Kind() == SyntaxKind.StaticKeyword);
                var yIsStatic = y.GetModifiers().Any(t => t.Kind() == SyntaxKind.StaticKeyword);

                if ((compare = Comparer<bool>.Default.Inverse().Compare(xIsStatic, yIsStatic)) != 0)
                {
                    return compare;
                }

                var xAccessibility = GetAccessibility(x);
                var yAccessibility = GetAccessibility(y);
                if ((compare = xAccessibility - yAccessibility) != 0)
                {
                    return compare;
                }

                var xName = ShouldCompareByName(x) ? x.GetNameToken() : default(SyntaxToken);
                var yName = ShouldCompareByName(y) ? y.GetNameToken() : default(SyntaxToken);

                if ((compare = TokenComparer.NormalInstance.Compare(xName, yName)) != 0)
                {
                    return compare;
                }

                // Their names were the same.  Order them by arity at this point.
                return x.GetArity() - y.GetArity();
            }

            private static Accessibility GetAccessibility(MemberDeclarationSyntax x)
            {
                var xModifiers = x.GetModifiers();

                if (xModifiers.Any(t => t.Kind() == SyntaxKind.PublicKeyword))
                {
                    return Accessibility.Public;
                }
                else if (xModifiers.Any(t => t.Kind() == SyntaxKind.ProtectedKeyword) && xModifiers.Any(t => t.Kind() == SyntaxKind.InternalKeyword))
                {
                    return Accessibility.ProtectedOrInternal;
                }
                else if (xModifiers.Any(t => t.Kind() == SyntaxKind.InternalKeyword))
                {
                    return Accessibility.Internal;
                }
                else if (xModifiers.Any(t => t.Kind() == SyntaxKind.ProtectedKeyword))
                {
                    return Accessibility.Protected;
                }
                else
                {
                    return Accessibility.Private;
                }
            }

            private static OuterOrdering GetOuterOrdering(MemberDeclarationSyntax x)
            {
                switch (x.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        return OuterOrdering.Fields;
                    case SyntaxKind.EventFieldDeclaration:
                        return OuterOrdering.EventFields;
                    case SyntaxKind.ConstructorDeclaration:
                        return OuterOrdering.Constructors;
                    case SyntaxKind.DestructorDeclaration:
                        return OuterOrdering.Destructors;
                    case SyntaxKind.PropertyDeclaration:
                        return OuterOrdering.Properties;
                    case SyntaxKind.EventDeclaration:
                        return OuterOrdering.Events;
                    case SyntaxKind.IndexerDeclaration:
                        return OuterOrdering.Indexers;
                    case SyntaxKind.OperatorDeclaration:
                        return OuterOrdering.Operators;
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return OuterOrdering.ConversionOperators;
                    case SyntaxKind.MethodDeclaration:
                        return OuterOrdering.Methods;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                        return OuterOrdering.Types;
                    default:
                        return OuterOrdering.Remaining;
                }
            }

            private static bool ShouldCompareByName(MemberDeclarationSyntax x)
            {
                // Constructors, destructors, indexers and operators should not be sorted by name.
                // Note:  Conversion operators should not be sorted by name either, but it's not
                //        necessary to deal with that here, because GetNameToken cannot return a
                //        name for them (there's only a NameSyntax, not a Token).
                switch (x.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                        return false;
                    default:
                        return true;
                }
            }
        }
    }
}
