// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CSharp.FindSymbols;

internal static class FindSymbolsUtilities
{
    public static Accessibility GetAccessibility(SyntaxNode container, SyntaxTokenList modifiers)
    {
        var sawInternal = false;
        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword: return Accessibility.Public;
                case SyntaxKind.PrivateKeyword: return Accessibility.Private;
                case SyntaxKind.ProtectedKeyword: return Accessibility.Protected;
                case SyntaxKind.InternalKeyword:
                    sawInternal = true;
                    continue;
            }
        }

        if (sawInternal)
            return Accessibility.Internal;

        // No accessibility modifiers:
        switch (container.Kind())
        {
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.ExtensionBlockDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
                // Anything without modifiers is private if it's in a class/struct declaration.
                return Accessibility.Private;
            case SyntaxKind.InterfaceDeclaration:
                // Anything without modifiers is public if it's in an interface declaration.
                return Accessibility.Public;
            case SyntaxKind.CompilationUnit:
                // Things are private by default in script
                if (((CSharpParseOptions)container.SyntaxTree.Options).Kind == SourceCodeKind.Script)
                    return Accessibility.Private;

                return Accessibility.Internal;

            default:
                // Otherwise it's internal
                return Accessibility.Internal;
        }
    }

    public static DeclaredSymbolInfoKind GetDeclaredSymbolInfoKind(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Kind() switch
        {
            SyntaxKind.ClassDeclaration => DeclaredSymbolInfoKind.Class,
            SyntaxKind.InterfaceDeclaration => DeclaredSymbolInfoKind.Interface,
            SyntaxKind.StructDeclaration => DeclaredSymbolInfoKind.Struct,
            SyntaxKind.RecordDeclaration => DeclaredSymbolInfoKind.Record,
            SyntaxKind.RecordStructDeclaration => DeclaredSymbolInfoKind.RecordStruct,
            _ => throw ExceptionUtilities.UnexpectedValue(typeDeclaration.Kind()),
        };
    }
}
