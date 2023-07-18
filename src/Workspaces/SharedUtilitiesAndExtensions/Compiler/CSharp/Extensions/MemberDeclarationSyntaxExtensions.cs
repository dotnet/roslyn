// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class MemberDeclarationSyntaxExtensions
    {
        private static readonly ConditionalWeakTable<MemberDeclarationSyntax, Dictionary<string, ImmutableArray<SyntaxToken>>> s_declarationCache = new();

        public static LocalDeclarationMap GetLocalDeclarationMap(this MemberDeclarationSyntax member)
            => new(s_declarationCache.GetValue(member, static member =>
            {
                var dictionary = DeclarationFinder.GetAllDeclarations(member);
                return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsImmutable());
            }));

        public static SyntaxToken GetNameToken(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).Identifier;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        return ((TypeDeclarationSyntax)member).Identifier;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Identifier;
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Declaration.Variables.First().Identifier;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Declaration.Variables.First().Identifier;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Identifier;
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Identifier;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Identifier;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).Identifier;
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).Identifier;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).ThisKeyword;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).OperatorToken;
                }
            }

            // Conversion operators don't have names.
            return default;
        }

        public static int GetArity(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        return ((TypeDeclarationSyntax)member).Arity;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Arity;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Arity;
                }
            }

            return 0;
        }

        public static TypeParameterListSyntax GetTypeParameterList(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        return ((TypeDeclarationSyntax)member).TypeParameterList;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).TypeParameterList;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).TypeParameterList;
                }
            }

            return null;
        }

        public static MemberDeclarationSyntax WithParameterList(
            this MemberDeclarationSyntax member,
            BaseParameterListSyntax parameterList)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).WithParameterList((ParameterListSyntax)parameterList);
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).WithParameterList((ParameterListSyntax)parameterList);
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).WithParameterList((ParameterListSyntax)parameterList);
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).WithParameterList((BracketedParameterListSyntax)parameterList);
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).WithParameterList((ParameterListSyntax)parameterList);
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).WithParameterList((ParameterListSyntax)parameterList);
                }
            }

            return null;
        }

        public static TypeSyntax GetMemberType(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).ReturnType;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).ReturnType;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).ReturnType;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Type;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).Type;
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Type;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Declaration.Type;
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Declaration.Type;
                }
            }

            return null;
        }

        public static bool HasMethodShape(this MemberDeclarationSyntax memberDeclaration)
            => memberDeclaration is BaseMethodDeclarationSyntax;

        public static BlockSyntax GetBody(this MemberDeclarationSyntax memberDeclaration)
            => memberDeclaration switch
            {
                BaseMethodDeclarationSyntax method => method.Body,
                _ => null,
            };

        public static ArrowExpressionClauseSyntax GetExpressionBody(this MemberDeclarationSyntax memberDeclaration)
            => memberDeclaration switch
            {
                BaseMethodDeclarationSyntax method => method.ExpressionBody,
                IndexerDeclarationSyntax indexer => indexer.ExpressionBody,
                PropertyDeclarationSyntax property => property.ExpressionBody,
                _ => null,
            };

        public static MemberDeclarationSyntax WithBody(this MemberDeclarationSyntax memberDeclaration, BlockSyntax body)
            => (memberDeclaration as BaseMethodDeclarationSyntax)?.WithBody(body);
    }
}
