// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum DeclarationKind : byte
    {
        Namespace,
        Class,
        Interface,
        Struct,
        Enum,
        Delegate,
        Script,
        Submission,
        ImplicitClass
    }

    internal static partial class EnumConversions
    {
        internal static DeclarationKind ToDeclarationKind(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration: return DeclarationKind.Class;
                case SyntaxKind.InterfaceDeclaration: return DeclarationKind.Interface;
                case SyntaxKind.StructDeclaration: return DeclarationKind.Struct;
                case SyntaxKind.NamespaceDeclaration: return DeclarationKind.Namespace;
                case SyntaxKind.EnumDeclaration: return DeclarationKind.Enum;
                case SyntaxKind.DelegateDeclaration: return DeclarationKind.Delegate;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
