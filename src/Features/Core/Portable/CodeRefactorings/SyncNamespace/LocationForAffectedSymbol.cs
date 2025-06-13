// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.ChangeNamespace;

internal abstract partial class AbstractChangeNamespaceService<
    TCompilationUnitSyntax,
    TMemberDeclarationSyntax,
    TNamespaceDeclarationSyntax,
    TNameSyntax,
    TSimpleNameSyntax,
    TCrefSyntax> where TCompilationUnitSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : TMemberDeclarationSyntax
    where TNameSyntax : SyntaxNode
    where TSimpleNameSyntax : TNameSyntax
    where TCrefSyntax : SyntaxNode
{
    private readonly struct LocationForAffectedSymbol(ReferenceLocation location, bool isReferenceToExtensionMethod)
    {
        public ReferenceLocation ReferenceLocation { get; } = location;

        public bool IsReferenceToExtensionMethod { get; } = isReferenceToExtensionMethod;

        public Document Document => ReferenceLocation.Document;
    }
}
