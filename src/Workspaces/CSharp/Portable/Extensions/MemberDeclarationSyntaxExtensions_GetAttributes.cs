// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class MemberDeclarationSyntaxExtensions
    {
        public static SyntaxList<AttributeListSyntax> GetAttributes(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
#if !CODE_STYLE
                return member.AttributeLists;
#else
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.EnumMemberDeclaration:
                        return ((EnumMemberDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).AttributeLists;
                    case SyntaxKind.IncompleteMember:
                        return ((IncompleteMemberSyntax)member).AttributeLists;
                }
#endif
            }

            return SyntaxFactory.List<AttributeListSyntax>();
        }
    }
}
