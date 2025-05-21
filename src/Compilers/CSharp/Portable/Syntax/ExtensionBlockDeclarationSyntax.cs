// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ExtensionBlockDeclarationSyntax
    {
        public override SyntaxToken Identifier => default;

        internal override BaseTypeDeclarationSyntax WithIdentifierCore(SyntaxToken identifier)
            => throw new System.NotSupportedException();

        public override BaseListSyntax? BaseList => null;

        internal override BaseTypeDeclarationSyntax AddBaseListTypesCore(params BaseTypeSyntax[] items)
            => throw new System.NotSupportedException();

        internal override BaseTypeDeclarationSyntax WithBaseListCore(BaseListSyntax? baseList)
            => throw new System.NotSupportedException();
    }
}
