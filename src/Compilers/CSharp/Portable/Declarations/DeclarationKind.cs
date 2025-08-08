// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        ImplicitClass,
        Record,
        RecordStruct,
        Extension,
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
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    return DeclarationKind.Namespace;
                case SyntaxKind.EnumDeclaration: return DeclarationKind.Enum;
                case SyntaxKind.DelegateDeclaration: return DeclarationKind.Delegate;
                case SyntaxKind.RecordDeclaration: return DeclarationKind.Record;
                case SyntaxKind.RecordStructDeclaration: return DeclarationKind.RecordStruct;
                case SyntaxKind.ExtensionBlockDeclaration: return DeclarationKind.Extension;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
