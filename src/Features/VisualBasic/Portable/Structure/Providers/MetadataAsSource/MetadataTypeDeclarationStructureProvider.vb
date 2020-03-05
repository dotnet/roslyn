' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
    Friend Class MetadataTypeDeclarationStructureProvider
        Inherits AbstractMetadataAsSourceStructureProvider(Of TypeStatementSyntax)

        Protected Overrides Function GetEndToken(node As TypeStatementSyntax) As SyntaxToken
            Return If(node.Modifiers.Count > 0,
                      node.Modifiers.First(),
                      node.DeclarationKeyword)
        End Function
    End Class
End Namespace
