// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The scope within a documentation cref.  Contains the implicitly declared type parameters
    /// of the cref (see <see cref="CrefTypeParameterSymbol"/> for details).
    /// </summary>
    internal sealed class WithCrefTypeParametersBinder : WithTypeParametersBinder
    {
        private readonly CrefSyntax _crefSyntax;
        private MultiDictionary<string, TypeParameterSymbol> _lazyTypeParameterMap;

        internal WithCrefTypeParametersBinder(CrefSyntax crefSyntax, Binder next)
            : base(next)
        {
            _crefSyntax = crefSyntax;
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (_lazyTypeParameterMap == null)
                {
                    MultiDictionary<string, TypeParameterSymbol> map = CreateTypeParameterMap();
                    Interlocked.CompareExchange(ref _lazyTypeParameterMap, map, null);
                }

                return _lazyTypeParameterMap;
            }
        }

        private MultiDictionary<string, TypeParameterSymbol> CreateTypeParameterMap()
        {
            var map = new MultiDictionary<string, TypeParameterSymbol>();

            switch (_crefSyntax.Kind())
            {
                case SyntaxKind.TypeCref:
                    {
                        AddTypeParameters(((TypeCrefSyntax)_crefSyntax).Type, map);
                        break;
                    }
                case SyntaxKind.QualifiedCref:
                    {
                        QualifiedCrefSyntax qualifiedCrefSyntax = ((QualifiedCrefSyntax)_crefSyntax);
                        AddTypeParameters(qualifiedCrefSyntax.Member, map);
                        AddTypeParameters(qualifiedCrefSyntax.Container, map);
                        break;
                    }
                case SyntaxKind.NameMemberCref:
                case SyntaxKind.IndexerMemberCref:
                case SyntaxKind.OperatorMemberCref:
                case SyntaxKind.ConversionOperatorMemberCref:
                case SyntaxKind.ExtensionMemberCref:
                    {
                        AddTypeParameters((MemberCrefSyntax)_crefSyntax, map);
                        break;
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(_crefSyntax.Kind());
                    }
            }
            return map;
        }

        private void AddTypeParameters(TypeSyntax typeSyntax, MultiDictionary<string, TypeParameterSymbol> map)
        {
            switch (typeSyntax.Kind())
            {
                case SyntaxKind.AliasQualifiedName:
                    AddTypeParameters(((AliasQualifiedNameSyntax)typeSyntax).Name, map);
                    break;
                case SyntaxKind.QualifiedName:
                    // NOTE: Dev11 does not warn about duplication, it just matches parameter types to the
                    // *last* type parameter with the same name.  That's why we're iterating backwards.
                    QualifiedNameSyntax qualifiedNameSyntax = (QualifiedNameSyntax)typeSyntax;
                    AddTypeParameters(qualifiedNameSyntax.Right, map);
                    AddTypeParameters(qualifiedNameSyntax.Left, map);
                    break;
                case SyntaxKind.GenericName:
                    AddTypeParameters(((GenericNameSyntax)typeSyntax).TypeArgumentList.Arguments, map);
                    break;
                case SyntaxKind.IdentifierName:
                case SyntaxKind.PredefinedType:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(typeSyntax.Kind());
            }
        }

        private void AddTypeParameters(MemberCrefSyntax memberSyntax, MultiDictionary<string, TypeParameterSymbol> map)
        {
            // Other members have arity 0.
            if (memberSyntax is NameMemberCrefSyntax nameMemberCref)
            {
                AddTypeParameters(nameMemberCref.Name, map);
            }
            else if (memberSyntax is ExtensionMemberCrefSyntax extensionCref)
            {
                if (extensionCref.TypeArgumentList is { } extensionTypeArguments)
                {
                    AddTypeParameters(extensionTypeArguments.Arguments, map);
                }

                AddTypeParameters(extensionCref.Member, map);
            }
        }

        private static void AddTypeParameters(SeparatedSyntaxList<TypeSyntax> typeArguments, MultiDictionary<string, TypeParameterSymbol> map)
        {
            // NOTE: Dev11 does not warn about duplication, it just matches parameter types to the
            // *last* type parameter with the same name.  That's why we're iterating backwards and
            // skipping subsequent symbols with the same name.  This can result in some surprising
            // behavior.  For example, both 'T's in "A<T>.B<T>" bind to the second implicitly
            // declared type parameter.
            for (int i = typeArguments.Count - 1; i >= 0; i--)
            {
                // Other types (non-identifiers) are allowed in error scenarios, but they do not introduce new 
                // cref type parameters.
                if (typeArguments[i].Kind() == SyntaxKind.IdentifierName)
                {
                    IdentifierNameSyntax typeParameterSyntax = (IdentifierNameSyntax)typeArguments[i];
                    Debug.Assert(typeParameterSyntax != null, "Syntactic requirement of crefs");

                    string name = typeParameterSyntax.Identifier.ValueText;
                    if (SyntaxFacts.IsValidIdentifier(name) && !map.ContainsKey(name))
                    {
                        TypeParameterSymbol typeParameterSymbol = new CrefTypeParameterSymbol(name, i, typeParameterSyntax);
                        map.Add(name, typeParameterSymbol);
                    }
                }
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (CanConsiderTypeParameters(options))
            {
                foreach (var kvp in TypeParameterMap)
                {
                    foreach (TypeParameterSymbol typeParameter in kvp.Value)
                    {
                        // In any context where this binder applies, the type parameters are always viable/speakable.
                        Debug.Assert(!result.CanBeAdded(typeParameter.Name) || originalBinder.CanAddLookupSymbolInfo(typeParameter, options, result, null));

                        result.AddSymbol(typeParameter, kvp.Key, 0);
                    }
                }
            }
        }
    }
}
