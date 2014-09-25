// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class TypeDeclarationSyntaxExtensions
    {
        public static ParameterListSyntax GetParameterList(this TypeDeclarationSyntax typeDeclaration)
        {
            Debug.Assert(typeDeclaration != null);

            switch (typeDeclaration.CSharpKind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)typeDeclaration).ParameterList;

                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)typeDeclaration).ParameterList;

                default:
                    return null;
            }
        }
    }
}