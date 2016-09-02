' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
    Friend Class MetadataEnumMemberDeclarationStructureProvider
        Inherits AbstractMetadataAsSourceStructureProvider(Of EnumMemberDeclarationSyntax)

        Protected Overrides Function GetEndToken(node As EnumMemberDeclarationSyntax) As SyntaxToken
            Return node.Identifier
        End Function
    End Class
End Namespace