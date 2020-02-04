// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static readonly ConditionalWeakTable<MemberDeclarationSyntax, Dictionary<string, ImmutableArray<SyntaxToken>>> s_declarationCache =
            new ConditionalWeakTable<MemberDeclarationSyntax, Dictionary<string, ImmutableArray<SyntaxToken>>>();
        private static readonly ConditionalWeakTable<MemberDeclarationSyntax, Dictionary<string, ImmutableArray<SyntaxToken>>>.CreateValueCallback s_createLocalDeclarationMap = CreateLocalDeclarationMap;

        public static LocalDeclarationMap GetLocalDeclarationMap(this MemberDeclarationSyntax member)
        {
            var result = s_declarationCache.GetValue(member, s_createLocalDeclarationMap);
            return new LocalDeclarationMap(result);
        }

        private static Dictionary<string, ImmutableArray<SyntaxToken>> CreateLocalDeclarationMap(MemberDeclarationSyntax member)
        {
            var dictionary = DeclarationFinder.GetAllDeclarations(member);
            return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsImmutable<SyntaxToken>());
        }

        public static int GetArity(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).Arity;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Arity;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Arity;
                }
            }

            return 0;
        }

        public static MemberDeclarationSyntax AddAttributeLists(
            this MemberDeclarationSyntax member,
            params AttributeListSyntax[] attributeLists)
        {
            return member.WithAttributeLists(member.GetAttributes().AddRange(attributeLists));
        }

        public static MemberDeclarationSyntax WithAttributeLists(
            this MemberDeclarationSyntax member,
            SyntaxList<AttributeListSyntax> attributeLists)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.EnumMemberDeclaration:
                        return ((EnumMemberDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).WithAttributeLists(attributeLists);
                    case SyntaxKind.IncompleteMember:
                        return ((IncompleteMemberSyntax)member).WithAttributeLists(attributeLists);
                }
            }

            return null;
        }

        public static bool HasMethodShape(this MemberDeclarationSyntax memberDeclaration)
        {
            if (memberDeclaration != null)
            {
                switch (memberDeclaration.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                        return true;
                }
            }

            return false;
        }

        public static BlockSyntax GetBody(this MemberDeclarationSyntax memberDeclaration)
        {
            if (memberDeclaration != null)
            {
                switch (memberDeclaration.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)memberDeclaration).Body;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)memberDeclaration).Body;
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)memberDeclaration).Body;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)memberDeclaration).Body;
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)memberDeclaration).Body;
                }
            }

            return null;
        }

        public static ArrowExpressionClauseSyntax GetExpressionBody(this MemberDeclarationSyntax memberDeclaration)
        {
            switch (memberDeclaration?.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)memberDeclaration).ExpressionBody;
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)memberDeclaration).ExpressionBody;
                default:
                    return null;
            }
        }

        public static MemberDeclarationSyntax WithBody(
            this MemberDeclarationSyntax memberDeclaration,
            BlockSyntax body)
        {
            if (memberDeclaration != null)
            {
                switch (memberDeclaration.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)memberDeclaration).WithBody(body);
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)memberDeclaration).WithBody(body);
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)memberDeclaration).WithBody(body);
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)memberDeclaration).WithBody(body);
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)memberDeclaration).WithBody(body);
                }
            }

            return null;
        }
    }
}
